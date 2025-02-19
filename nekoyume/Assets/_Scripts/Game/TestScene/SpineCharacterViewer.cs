using Nekoyume.Game;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Util;
using Nekoyume.UI.Module;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.TestScene
{
    public class SpineCharacterViewer : MonoBehaviour
    {
        [SerializeField]
        private GameObject menus;

        [SerializeField]
        private Transform cameraTransform;

        [SerializeField]
        private Transform bgParent;

        [SerializeField]
        private float cameraSpeed;

        [SerializeField]
        private ObjectPool objectPool;

        [SerializeField]
        private TMP_InputField spinePrefabIDField;

        [SerializeField]
        private TMP_InputField backgroundPrefabIDField;

        [SerializeField]
        private TextMeshProUGUI resourceWarningText;

        [SerializeField]
        private Button loadSpineButton;

        [SerializeField]
        private Button loadBgButton;

        [SerializeField]
        private Enemy enemy;

        [SerializeField]
        private NPC npc;

        [SerializeField]
        private Player player;

        [SerializeField]
        private Transform animationButtonParent;

        [SerializeField]
        private TextButton animationButtonPrefab;

        private readonly Queue<TextButton> _buttonPool = new Queue<TextButton>();

        private readonly Queue<TextButton> _activeButtons = new Queue<TextButton>();

        private GameObject _background;

        #region Mono

        private void Awake()
        {
            loadSpineButton.onClick.AddListener(LoadSpineObject);
            loadBgButton.onClick.AddListener(LoadBackground);
            resourceWarningText.gameObject.SetActive(false);
            AudioController.instance.Initialize();
            objectPool.Initialize();
        }

        private void Update()
        {
            var h = Input.GetAxis("Horizontal");
            var v = Input.GetAxis("Vertical");

            cameraTransform.Translate(new Vector3(h, v) * Time.fixedDeltaTime * cameraSpeed);

            if (Input.GetKeyDown(KeyCode.Space))
            {
                cameraTransform.position = new Vector3(0f, 0f, -100f);
            }
        }

        #endregion

        public void ToggleUI()
        {
            menus.SetActive(!menus.activeSelf);
        }

        private void LoadSpineObject()
        {
            enemy.gameObject.SetActive(false);
            npc.gameObject.SetActive(false);
            player.gameObject.SetActive(false);
            resourceWarningText.gameObject.SetActive(false);

            var text = spinePrefabIDField.text;
            if (string.IsNullOrEmpty(text))
            {
                resourceWarningText.text = "Resource ID is empty.";
                resourceWarningText.gameObject.SetActive(true);
                return;
            }

            try
            {
                if (IsMonster(text))
                {
                    ShowMonster(text);
                }
                else if (IsNPC(text))
                {
                    ShowNPC(text);
                }
                else if (IsPlayer(text))
                {
                    ShowPlayer(text);
                }
                else if (IsFullCostume(text))
                {
                    ShowFullCostume(text);
                }
                else
                {
                    resourceWarningText.text = "Prefab name is invaild.";
                    resourceWarningText.gameObject.SetActive(true);
                    return;
                }
            }
            catch (FailedToLoadResourceException<GameObject> e)
            {
                resourceWarningText.text = e.Message;
                resourceWarningText.gameObject.SetActive(true);
            }
        }

        private void ShowMonster(string id)
        {
            enemy.gameObject.SetActive(true);
            enemy.ChangeSpineResource(id);
            ShowCharacterAnimations(enemy.Animator);
        }

        private void ShowNPC(string id)
        {
            npc.gameObject.SetActive(true);
            npc.ChangeSpineResource(id);
            ShowCharacterAnimations(npc.Animator, true);
        }

        private void ShowPlayer(string id)
        {
            player.gameObject.SetActive(true);
            player.ChangeSpineResource(id, false);
            ShowCharacterAnimations(player.Animator);
        }

        private void ShowFullCostume(string id)
        {
            player.gameObject.SetActive(true);
            player.ChangeSpineResource(id, true);
            ShowCharacterAnimations(player.Animator);
        }

        private void LoadBackground()
        {
            var prefabName = backgroundPrefabIDField.text;

            if (_background)
            {
                if (_background.name.Equals(prefabName))
                    return;

                DestroyBackground();
            }

            try
            {
                var path = $"Prefab/Background/{prefabName}";
                var prefab = Resources.Load<GameObject>(path);
                if (!prefab)
                    throw new FailedToLoadResourceException<GameObject>(path);
                _background = Instantiate(prefab, bgParent);
                _background.name = prefabName;
            }
            catch (FailedToLoadResourceException<GameObject> e)
            {
                resourceWarningText.text = e.Message;
                resourceWarningText.gameObject.SetActive(true);
            }
        }

        private void DestroyBackground()
        {
            Destroy(_background);
            _background = null;
        }

        private void ShowCharacterAnimations(SkeletonAnimator animator, bool isNpc = false)
        {
            resourceWarningText.gameObject.SetActive(false);

            while (_activeButtons.Count > 0)
            {
                var button = _activeButtons.Dequeue();
                button.gameObject.SetActive(false);
                _buttonPool.Enqueue(button);
            }

            var types = Enum.GetValues(isNpc ?
                typeof(NPCAnimation.Type) : typeof(CharacterAnimation.Type));

            foreach (var type in types)
            {
                if (_buttonPool.Count == 0)
                {
                    var newButton = Instantiate(animationButtonPrefab, animationButtonParent);
                    _buttonPool.Enqueue(newButton);
                }

                var button = _buttonPool.Dequeue();
                if (isNpc)
                {
                    var animationType = (NPCAnimation.Type)type;
                    button.Text = animationType.ToString();
                    var npcAnimator = animator as NPCAnimator;
                    button.OnClick = () =>
                    {
                        resourceWarningText.gameObject.SetActive(false);
                        if (!npcAnimator.HasType(animationType))
                        {
                            resourceWarningText.text = $"Animation not found.\nType : {animationType}";
                            resourceWarningText.gameObject.SetActive(true);
                            return;
                        }
                        npcAnimator.Play(animationType);
                    };
                }
                else
                {
                    var animationType = (CharacterAnimation.Type)type;
                    button.Text = animationType.ToString();
                    var characterAnimator = animator as CharacterAnimator;
                    button.OnClick = () =>
                    {
                        resourceWarningText.gameObject.SetActive(false);
                        if (!characterAnimator.HasType(animationType))
                        {
                            resourceWarningText.text = $"Animation not found.\nType : {animationType}";
                            resourceWarningText.gameObject.SetActive(true);
                            return;
                        }
                        characterAnimator.Play(animationType);
                    };
                }

                button.gameObject.SetActive(true);
                _activeButtons.Enqueue(button);
            }
        }

        #region Check Type

        private bool IsPlayer(string prefabName)
        {
            return prefabName.StartsWith("1");
        }

        private bool IsMonster(string prefabName)
        {
            return prefabName.StartsWith("2");
        }

        private bool IsNPC(string prefabName)
        {
            return prefabName.StartsWith("3") ||
                prefabName.StartsWith("dialog_");
        }

        private bool IsFullCostume(string prefabName)
        {
            return prefabName.StartsWith("4");
        }

        #endregion
    }
}
