using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Entities.Areas
{
    public class GroundBattleField : MonoBehaviour
    {
        [SerializeField] private Transform _flag;
        [SerializeField] private List<Transform> _enemies = new();
        [SerializeField] private float _groundHeight = 0.02f;
        [SerializeField] private float _dashGroundHeight = 0.04f;
        [SerializeField] private float _frontPadding = 0.85f;
        [SerializeField] private float _backPadding = 0.65f;
        [SerializeField] private float _sidePadding = 0.85f;
        [SerializeField] private float _minWidth = 2.2f;
        [SerializeField] private float _minLength = 3.4f;
        [SerializeField] private int _capSegments = 14;
        [SerializeField] private Material _fillMaterial;
        [SerializeField] private Material _dashMaterial;
        [SerializeField] private float _dashLength = 0.34f;
        [SerializeField] private float _dashGap = 0.32f;
        [SerializeField] private float _dashWidth = 0.13f;
        [SerializeField] private float _smoothTime = 0.12f;
        [SerializeField] private Color _fillColor = new(0.72f, 0.42f, 0.25f, 0.72f);
        [SerializeField] private Color _dashColor = Color.white;

        private readonly List<Vector3> _points = new();
        private readonly List<Vector3> _outline = new();
        private readonly List<Vector3> _vertices = new();
        private readonly List<int> _triangles = new();

        private Mesh _fillMesh;
        private Mesh _dashMesh;
        private Vector3 _currentCenter;
        private Vector3 _centerVelocity;
        private Vector2 _currentSize;
        private Vector2 _sizeVelocity;
        private Vector3 _currentForward = Vector3.forward;

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

            Rebuild(true);
        }

        private void LateUpdate()
        {
            Rebuild(false);
        }

        private void CreateRenderer(string objectName, Mesh mesh, Material material, int sortingOrder)
        {
            GameObject child = new(objectName);
            child.transform.SetParent(transform, false);

            MeshFilter meshFilter = child.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = child.AddComponent<MeshRenderer>();

            meshFilter.sharedMesh = mesh;
            meshRenderer.sharedMaterial = CreateMaterialInstance(material, sortingOrder == 0 ? _fillColor : _dashColor);
            meshRenderer.sortingOrder = sortingOrder;
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

        private void Rebuild(bool instant)
        {
            CollectPoints();
            CalculateTargetShape(out Vector3 targetCenter, out Vector2 targetSize, out Vector3 targetForward);

            if (instant)
            {
                _currentCenter = targetCenter;
                _currentSize = targetSize;
                _currentForward = targetForward;
            }
            else
            {
                _currentCenter = Vector3.SmoothDamp(_currentCenter, targetCenter, ref _centerVelocity, _smoothTime);
                _currentSize = Vector2.SmoothDamp(_currentSize, targetSize, ref _sizeVelocity, _smoothTime);
                _currentForward = Vector3.Slerp(_currentForward, targetForward, Time.deltaTime / _smoothTime);
            }

            BuildOutline(_currentCenter, _currentSize, _currentForward);
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

        private void CalculateTargetShape(out Vector3 center, out Vector2 size, out Vector3 forward)
        {
            Vector3 flagPoint = transform.InverseTransformPoint(_flag.position);
            Vector3 enemiesCenter = flagPoint + transform.forward;

            if (_enemies.Count > 0)
            {
                enemiesCenter = Vector3.zero;

                for (int i = 0; i < _enemies.Count; i++)
                {
                    enemiesCenter += transform.InverseTransformPoint(_enemies[i].position);
                }

                enemiesCenter /= _enemies.Count;
            }

            forward = enemiesCenter - flagPoint;
            forward.y = 0f;

            if (forward.sqrMagnitude < 0.001f)
            {
                forward = _currentForward;
            }
            else
            {
                forward.Normalize();
            }

            Vector3 side = new(forward.z, 0f, -forward.x);
            float minForward = 0f;
            float maxForward = 0f;
            float minSide = 0f;
            float maxSide = 0f;

            for (int i = 0; i < _points.Count; i++)
            {
                Vector3 point = transform.InverseTransformPoint(_points[i]) - flagPoint;
                float forwardDistance = Vector3.Dot(point, forward);
                float sideDistance = Vector3.Dot(point, side);

                minForward = Mathf.Min(minForward, forwardDistance);
                maxForward = Mathf.Max(maxForward, forwardDistance);
                minSide = Mathf.Min(minSide, sideDistance);
                maxSide = Mathf.Max(maxSide, sideDistance);
            }

            minForward -= _backPadding;
            maxForward += _frontPadding;
            minSide -= _sidePadding;
            maxSide += _sidePadding;

            float centerForward = (minForward + maxForward) * 0.5f;
            float centerSide = (minSide + maxSide) * 0.5f;

            center = flagPoint + forward * centerForward + side * centerSide;
            center.y = _groundHeight;
            size = new Vector2(
                Mathf.Max(_minWidth, maxSide - minSide),
                Mathf.Max(_minLength, maxForward - minForward));
        }

        private void BuildOutline(Vector3 center, Vector2 size, Vector3 forward)
        {
            _outline.Clear();

            float radius = size.x * 0.5f;
            float halfStraightLength = Mathf.Max(0f, (size.y - size.x) * 0.5f);
            Vector3 side = new(forward.z, 0f, -forward.x);
            Vector3 frontCenter = center + forward * halfStraightLength;
            Vector3 backCenter = center - forward * halfStraightLength;

            AddCap(frontCenter, forward, side, radius, true);
            AddCap(backCenter, forward, side, radius, false);
        }

        private void AddCap(Vector3 center, Vector3 forward, Vector3 side, float radius, bool isFront)
        {
            for (int i = 0; i <= _capSegments; i++)
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
                _triangles.Add(i);
                _triangles.Add(next);
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

            float step = _dashLength + _dashGap;
            float distance = 0f;

            for (int i = 0; i < _outline.Count; i++)
            {
                Vector3 from = _outline[i];
                Vector3 to = _outline[(i + 1) % _outline.Count];
                float edgeLength = Vector3.Distance(from, to);
                Vector3 direction = (to - from).normalized;

                while (distance < edgeLength)
                {
                    float dashEnd = Mathf.Min(distance + _dashLength, edgeLength);
                    AddDash(from + direction * distance, from + direction * dashEnd);
                    distance += step;
                }

                distance -= edgeLength;
            }

            _dashMesh.Clear();
            _dashMesh.SetVertices(_vertices);
            _dashMesh.SetTriangles(_triangles, 0);
            _dashMesh.RecalculateBounds();
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
