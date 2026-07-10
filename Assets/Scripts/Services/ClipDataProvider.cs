using System;
using System.Linq;
using UnityEngine;

namespace Services
{
    public static class ClipDataProvider
    {
        public static float ClipDuration(Animator animator, string clipName)
        {
            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
            AnimationClip targetClip = clips.FirstOrDefault(animationClip => animationClip.name == clipName);
            if (targetClip != null)
            {
                return targetClip.length;
            }
            throw new ArgumentOutOfRangeException("Can't find clip of name " + clipName);
        }
    }
}