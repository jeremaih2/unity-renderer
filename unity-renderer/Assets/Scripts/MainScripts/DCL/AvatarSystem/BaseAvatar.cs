using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AvatarSystem
{
    public class BaseAvatar : IBaseAvatar
    {
        public IBaseAvatarRevealer avatarRevealer { get; set; }
        private ILOD lod;
        private Transform avatarRevealerContainer;//avatar显示容器
        private CancellationTokenSource transitionCts = new CancellationTokenSource();

        public GameObject armatureContainer;//人形骨骼容器
        public SkinnedMeshRenderer meshRenderer { get; private set; }//人物蒙皮网格渲染

        public BaseAvatar(Transform avatarRevealerContainer, GameObject armatureContainer, ILOD lod) 
        {
            this.avatarRevealerContainer = avatarRevealerContainer;
            this.armatureContainer = armatureContainer;
            this.lod = lod;
        }
//获取人形骨骼容器
        public GameObject GetArmatureContainer()
        {
            return armatureContainer;
        }
//获取主要的渲染
        public SkinnedMeshRenderer GetMainRenderer()
        {
            return avatarRevealer.GetMainRenderer();
        }

        public void Initialize() 
        {
            if (avatarRevealer == null)
            {
                avatarRevealer = UnityEngine.Object.Instantiate(Resources.Load<GameObject>("LoadingAvatar"), avatarRevealerContainer).GetComponent<BaseAvatarReveal>();
                avatarRevealer.InjectLodSystem(lod);//注入lod系统
            }
            else
            {
                avatarRevealer.Reset();//重置，清零
            }

            meshRenderer = avatarRevealer.GetMainRenderer();
        }
        //人物淡入淡出特效
        public async UniTask FadeOut(MeshRenderer targetRenderer, bool withTransition, CancellationToken cancellationToken)
        {
            if (avatarRevealerContainer == null)
                return;

            transitionCts ??= new CancellationTokenSource();
            CancellationToken linkedCt = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, transitionCts.Token).Token;
            linkedCt.ThrowIfCancellationRequested();

            avatarRevealer.AddTarget(targetRenderer);
            //If canceled, the final state of the avatar is handle inside StartAvatarRevealAnimation
            await avatarRevealer.StartAvatarRevealAnimation(withTransition, linkedCt);

            transitionCts?.Dispose();
            transitionCts = null;
        }
        public void CancelTransition()
        {
            transitionCts?.Cancel();
            transitionCts?.Dispose();
            transitionCts = null;
        }

    }
}
