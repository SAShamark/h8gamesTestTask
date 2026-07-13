using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace Game.Entities.Areas
{
    public class GroundBattleField : MonoBehaviour
    {
        [SerializeField] private Transform _flag;
        [SerializeField] private List<Transform> _enemies = new();
        [SerializeField] private float _groundHeight = 0.02f;
        [SerializeField] private float _dashGroundHeight = 0.04f;
        [FormerlySerializedAs("_sidePadding")]
        [SerializeField] private float _outlinePadding = 0.85f;
        [SerializeField] private int _capSegments = 14;
        [SerializeField] private Material _fillMaterial;
        [SerializeField] private Material _dashMaterial;
        [SerializeField] private float _dashLength = 0.34f;
        [SerializeField] private float _dashGap = 0.32f;
        [SerializeField] private float _dashWidth = 0.13f;
        [SerializeField] private Color _fillColor = new(0.72f, 0.42f, 0.25f, 0.72f);
        [SerializeField] private Color _dashColor = Color.white;

        private readonly List<Vector3> _points = new();
        private readonly List<Vector3> _hull = new();
        private readonly List<Vector3> _outline = new();
        private readonly List<Vector3> _vertices = new();
        private readonly List<int> _triangles = new();

        private Mesh _fillMesh;
        private Mesh _dashMesh;
        private Vector3 _currentCenter;

        public void AddEnemy(Transform enemy)
        {
            if (!_enemies.Contains(enemy))
            {
                _enemies.Add(enemy);
            }
        }

        public void RemoveEnemy(Transform enemy)
        {
            _enemies.Remove(enemy);
        }

        private void Awake()
        {
            _fillMesh = new Mesh { name = "Ground Battle Field Fill" };
            _dashMesh = new Mesh { name = "Ground Battle Field Dashes" };

            CreateRenderer("Fill", _fillMesh, _fillMaterial, 0);
            CreateRenderer("Dashes", _dashMesh, _dashMaterial, 1);

            Rebuild();
        }

        private void LateUpdate()
        {
            Rebuild();
        }

        private void CreateRenderer(string objectName, Mesh mesh, Material material, int sortingOrder)
        {
            GameObject child = new(objectName);
            child.transform.SetParent(transform, false);

            MeshFilter meshFilter = child.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = child.AddComponent<MeshRenderer>();

            meshFilter.sharedMesh = mesh;
            meshRenderer.sharedMaterial = sortingOrder == 0
                ? CreateMaterialInstance(material, _fillColor)
                : CreateUnlitMaterial(material, _dashColor);
            meshRenderer.sortingOrder = sortingOrder;
        }

        private Material CreateUnlitMaterial(Material sourceMaterial, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            Material material = sourceMaterial != null ? new Material(sourceMaterial) : new Material(shader);
            material.shader = shader;
            ApplyMaterialColor(material, color);

            return material;
        }

        private Material CreateMaterialInstance(Material sourceMaterial, Color color)
        {
            Material material = sourceMaterial != null ? new Material(sourceMaterial) : CreateDefaultMaterial(color);
            ApplyMaterialColor(material, color);

            if (color.a < 1f)
            {
                MakeTransparent(material);
            }

            return material;
        }

        private Material CreateDefaultMaterial(Color color)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            }

            Material material = new(shader);

            return material;
        }

        private void ApplyMaterialColor(Material material, Color color)
        {
            material.color = color;

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
        }

        private void MakeTransparent(Material material)
        {
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 0f);
            }

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }

        private void Rebuild()
        {
            CollectPoints();
            BuildRoundedHull();
            BuildFillMesh();
            BuildDashMesh();
        }

        private void CollectPoints()
        {
            _points.Clear();
            _points.Add(_flag.position);

            for (int i = 0; i < _enemies.Count; i++)
            {
                _points.Add(_enemies[i].position);
            }
        }

        private void BuildRoundedHull()
        {
            _outline.Clear();
            BuildConvexHull();

            if (_hull.Count == 1)
            {
                AddCircle(_hull[0], _outlinePadding);
            }
            else if (_hull.Count == 2)
            {
                BuildCapsule(_hull[0], _hull[1], _outlinePadding);
            }
            else
            {
                for (int i = 0; i < _hull.Count; i++)
                {
                    Vector3 previous = _hull[(i - 1 + _hull.Count) % _hull.Count];
                    Vector3 current = _hull[i];
                    Vector3 next = _hull[(i + 1) % _hull.Count];
                    Vector3 incoming = (current - previous).normalized;
                    Vector3 outgoing = (next - current).normalized;
                    Vector3 firstNormal = new(incoming.z, 0f, -incoming.x);
                    Vector3 lastNormal = new(outgoing.z, 0f, -outgoing.x);

                    AddRoundedCorner(current, firstNormal, lastNormal, _outlinePadding);
                }
            }

            _currentCenter = Vector3.zero;
            for (int i = 0; i < _hull.Count; i++)
            {
                _currentCenter += _hull[i];
            }

            _currentCenter /= _hull.Count;
            _currentCenter.y = _groundHeight;
        }

        private void BuildConvexHull()
        {
            _hull.Clear();

            for (int i = 0; i < _points.Count; i++)
            {
                Vector3 point = transform.InverseTransformPoint(_points[i]);
                point.y = _groundHeight;
                _hull.Add(point);
            }

            _hull.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.z.CompareTo(b.z));

            if (_hull.Count <= 2)
            {
                return;
            }

            List<Vector3> sortedPoints = new(_hull);
            _hull.Clear();

            for (int i = 0; i < sortedPoints.Count; i++)
            {
                while (_hull.Count >= 2 && Cross(_hull[^2], _hull[^1], sortedPoints[i]) <= 0f)
                {
                    _hull.RemoveAt(_hull.Count - 1);
                }

                _hull.Add(sortedPoints[i]);
            }

            int lowerHullCount = _hull.Count;

            for (int i = sortedPoints.Count - 2; i >= 0; i--)
            {
                while (_hull.Count > lowerHullCount && Cross(_hull[^2], _hull[^1], sortedPoints[i]) <= 0f)
                {
                    _hull.RemoveAt(_hull.Count - 1);
                }

                _hull.Add(sortedPoints[i]);
            }

            _hull.RemoveAt(_hull.Count - 1);
        }

        private float Cross(Vector3 a, Vector3 b, Vector3 c)
        {
            return (b.x - a.x) * (c.z - a.z) - (b.z - a.z) * (c.x - a.x);
        }

        private void AddCircle(Vector3 center, float radius)
        {
            int segmentCount = _capSegments * 2;

            for (int i = 0; i < segmentCount; i++)
            {
                float angle = i / (float)segmentCount * Mathf.PI * 2f;
                _outline.Add(center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius);
            }
        }

        private void BuildCapsule(Vector3 start, Vector3 end, float radius)
        {
            Vector3 forward = (end - start).normalized;
            Vector3 side = new(forward.z, 0f, -forward.x);

            AddCap(end, forward, side, radius, true);
            AddCap(start, forward, side, radius, false);
        }

        private void AddRoundedCorner(Vector3 center, Vector3 firstNormal, Vector3 lastNormal, float radius)
        {
            float firstAngle = Mathf.Atan2(firstNormal.z, firstNormal.x);
            float lastAngle = Mathf.Atan2(lastNormal.z, lastNormal.x);
            float angleRange = Mathf.Repeat(lastAngle - firstAngle, Mathf.PI * 2f);
            int segmentCount = Mathf.Max(1, Mathf.CeilToInt(_capSegments * angleRange / Mathf.PI));

            for (int i = 0; i <= segmentCount; i++)
            {
                float angle = firstAngle + angleRange * i / segmentCount;
                _outline.Add(center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius);
            }
        }

        private void AddCap(Vector3 center, Vector3 forward, Vector3 side, float radius, bool isFront)
        {
            int firstPoint = isFront ? 0 : 1;
            int lastPoint = isFront ? _capSegments : _capSegments - 1;

            for (int i = firstPoint; i <= lastPoint; i++)
            {
                float t = i / (float)_capSegments * Mathf.PI;
                Vector3 direction = isFront
                    ? side * Mathf.Cos(t) + forward * Mathf.Sin(t)
                    : -side * Mathf.Cos(t) - forward * Mathf.Sin(t);
                Vector3 point = center + direction * radius;

                _outline.Add(point);
            }
        }

        private void BuildFillMesh()
        {
            _vertices.Clear();
            _triangles.Clear();

            _vertices.Add(_currentCenter);
            for (int i = 0; i < _outline.Count; i++)
            {
                _vertices.Add(_outline[i]);
            }

            for (int i = 1; i <= _outline.Count; i++)
            {
                int next = i == _outline.Count ? 1 : i + 1;
                _triangles.Add(0);
                _triangles.Add(next);
                _triangles.Add(i);
            }

            _fillMesh.Clear();
            _fillMesh.SetVertices(_vertices);
            _fillMesh.SetTriangles(_triangles, 0);
            _fillMesh.RecalculateBounds();
        }

        private void BuildDashMesh()
        {
            _vertices.Clear();
            _triangles.Clear();

            float perimeter = 0f;

            for (int i = 0; i < _outline.Count; i++)
            {
                perimeter += Vector3.Distance(_outline[i], _outline[(i + 1) % _outline.Count]);
            }

            int dashCount = Mathf.Max(1, Mathf.FloorToInt(perimeter / (_dashLength + _dashGap)));
            float evenlyDistributedGap = (perimeter - dashCount * _dashLength) / dashCount;
            float step = _dashLength + evenlyDistributedGap;

            for (int i = 0; i < dashCount; i++)
            {
                float dashStart = i * step;
                Vector3 start = GetOutlinePoint(dashStart);
                Vector3 end = GetOutlinePoint(dashStart + _dashLength);
                AddDash(start, end);
            }

            _dashMesh.Clear();
            _dashMesh.SetVertices(_vertices);
            _dashMesh.SetTriangles(_triangles, 0);
            _dashMesh.RecalculateBounds();
        }

        private Vector3 GetOutlinePoint(float distance)
        {
            for (int i = 0; i < _outline.Count; i++)
            {
                Vector3 from = _outline[i];
                Vector3 to = _outline[(i + 1) % _outline.Count];
                float edgeLength = Vector3.Distance(from, to);

                if (distance <= edgeLength)
                {
                    return Vector3.Lerp(from, to, distance / edgeLength);
                }

                distance -= edgeLength;
            }

            return _outline[0];
        }

        private void AddDash(Vector3 start, Vector3 end)
        {
            Vector3 direction = (end - start).normalized;
            Vector3 side = new(-direction.z, 0f, direction.x);
            Vector3 offset = side * (_dashWidth * 0.5f);
            int vertexIndex = _vertices.Count;

            start.y = _dashGroundHeight;
            end.y = _dashGroundHeight;

            _vertices.Add(start - offset);
            _vertices.Add(start + offset);
            _vertices.Add(end + offset);
            _vertices.Add(end - offset);

            _triangles.Add(vertexIndex);
            _triangles.Add(vertexIndex + 1);
            _triangles.Add(vertexIndex + 2);
            _triangles.Add(vertexIndex);
            _triangles.Add(vertexIndex + 2);
            _triangles.Add(vertexIndex + 3);
        }
    }
}
