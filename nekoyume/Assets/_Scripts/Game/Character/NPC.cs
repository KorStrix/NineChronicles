using System;
using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Spine.Unity;
using UnityEngine;
using UnityEngine.Rendering;

namespace Nekoyume.Game.Character
{
    using UniRx;

    [RequireComponent(typeof(SortingGroup))]
    public class NPC : MonoBehaviour
    {
        private const float AnimatorTimeScale = 1.2f;

        private SortingGroup _sortingGroup;
        private TouchHandler _touchHandler;

        private NPCAnimator Animator { get; set; }
        public NPCSpineController SpineController { get; private set; }

        private void Awake()
        {
            _sortingGroup = GetComponent<SortingGroup>();
            _touchHandler = GetComponentInChildren<TouchHandler>();

            _touchHandler.OnClick
                .Merge(_touchHandler.OnDoubleClick)
                .Merge(_touchHandler.OnMultipleClick)
                .Subscribe(_ => PlayAnimation(NPCAnimation.Type.Touch_01))
                .AddTo(gameObject);

            Animator = new NPCAnimator(this) {TimeScale = AnimatorTimeScale};
            Animator.OnEvent.Subscribe(OnAnimatorEvent);
        }

        private void Start()
        {
            UpdateAnimatorTarget();
        }

        public void SetSortingLayer(LayerType layerType)
        {
            SetSortingLayer(layerType, _sortingGroup.sortingOrder);
        }

        public void SetSortingLayer(LayerType layerType, int sortingOrder)
        {
            _sortingGroup.sortingLayerName = layerType.ToLayerName();
            _sortingGroup.sortingOrder = sortingOrder;
        }

        public void ResetAnimatorTarget(GameObject target)
        {
            if (target is null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            Animator.ResetTarget(target);
        }

        public void PlayAnimation(NPCAnimation.Type type)
        {
            Animator.Play(type);
        }

        private void UpdateAnimatorTarget()
        {
            var target = GetComponentInChildren<NPCSpineController>();
            if (target is null)
            {
                return;
            }

        }

        protected void OnAnimatorEvent(string eventName)
        {
            switch (eventName)
            {
                case "Smash":
                {
                    AudioController.instance.PlaySfx(AudioController.SfxCode.CombinationSmash);
                    var position = ActionCamera.instance.Cam.transform.position;
                    VFXController.instance.CreateAndChaseCam<HammerSmashVFX>(
                        position,
                        new Vector3(0.7f, -0.25f));
                    break;
                }
                case "emotion":
                {
                    break;
                }
            }
        }
    }
}
