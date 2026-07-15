using System;
using Game.Entities;
using Game.Entities.Areas;
using Game.Entities.Units;
using Game.Entities.Units.Character;
using UI.Managers;
using UI.Popups;
using UI.Popups.Variables;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game
{
    public class GameplayController : IDisposable
    {
        private readonly CharacterControl _characterControl;
        private readonly UnitsManager _unitsManager;
        private readonly FlagCaptureArea _flagCaptureArea;
        private readonly UIManager _uiManager;
        private readonly Vector3 _characterStartPosition;
        private readonly Quaternion _characterStartRotation;

        public GameplayController(CharacterControl characterControl, UnitsManager unitsManager,
            FlagCaptureArea flagCaptureArea)
        {
            _characterControl = characterControl;
            _unitsManager = unitsManager;
            _flagCaptureArea = flagCaptureArea;
            _uiManager = UIManager.Instance;
            _characterStartPosition = characterControl.transform.position;
            _characterStartRotation = characterControl.transform.rotation;
        }

        public void Init()
        {
            _unitsManager.OnEnemiesDefeated += HandleEnemiesDefeated;
            _flagCaptureArea.OnCaptured += HandleFlagCaptured;
            _characterControl.Health.OnDeath += HandleCharacterDied;
        }

        public void Dispose()
        {
            _unitsManager.OnEnemiesDefeated -= HandleEnemiesDefeated;
            _flagCaptureArea.OnCaptured -= HandleFlagCaptured;
            _characterControl.Health.OnDeath -= HandleCharacterDied;
        }

        private void HandleCharacterDied()
        {
            _unitsManager.PlayEnemiesVictory();
            _uiManager.PopupsManager.ShowPopup(PopupTypes.Result);

            ResultPopup resultPopup = (ResultPopup)_uiManager.PopupsManager.GetPopup(PopupTypes.Result);
            resultPopup.OnButtonClicked += HandleResultRestartClicked;
        }

        private void HandleEnemiesDefeated()
        {
            _flagCaptureArea.Unlock();
        }

        private void HandleFlagCaptured()
        {
            _characterControl.PlayVictory();
            _unitsManager.PlayTeammatesVictory();
            _uiManager.PopupsManager.ShowPopup(PopupTypes.LevelComplete);

            LevelCompletedPopup levelCompletedPopup =
                (LevelCompletedPopup)_uiManager.PopupsManager.GetPopup(PopupTypes.LevelComplete);
            levelCompletedPopup.OnButtonClicked += HandleLevelCompleteRestartClicked;
        }

        private void HandleResultRestartClicked()
        {
            ResultPopup resultPopup = (ResultPopup)_uiManager.PopupsManager.GetPopup(PopupTypes.Result);
            resultPopup.OnButtonClicked -= HandleResultRestartClicked;

            _uiManager.PopupsManager.HidePopup(PopupTypes.Result);
            _characterControl.Respawn(_characterStartPosition, _characterStartRotation);
            _unitsManager.ResumeAfterCharacterRespawn();
        }

        private void HandleLevelCompleteRestartClicked()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
