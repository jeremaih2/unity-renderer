using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AvatarSystem
{
    public class BaseAvatar : IBaseAvatar
    {
        public IBaseAvatarRevealer avatarRevealer { get; set; }
        private ILOD lod;
        private Transform avatarRevealerContainer;//avatar显示容器
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
        public void FadeOut(MeshRenderer targetRenderer, bool playParticles) 
        {
            if (avatarRevealerContainer == null) 
                return;

            avatarRevealer.AddTarget(targetRenderer);
            avatarRevealer.StartAvatarRevealAnimation(playParticles);
        }

    }
}
