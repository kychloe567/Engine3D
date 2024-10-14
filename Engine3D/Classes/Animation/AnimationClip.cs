﻿using Assimp;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace Engine3D
{
    public class AnimationClip
    {
        public double DurationInTicks;
        public double TicksPerSecond;
        public string Name;

        private Dictionary<string, AnimationPose> BoneMapping = new Dictionary<string, AnimationPose>();
        private double LocalTimer = 0.0f;
        //private float BlendUpdateRatio = 1.0f;

        public Dictionary<string, Matrix4> AnimationMatrices = new Dictionary<string, Matrix4>();

        public AnimationClip() { }

        public void AnimateKeyFrames(Bone bone, double animTime)
        {
            throw new NotImplementedException();

            //Matrix4 localAnim = Matrix4.Identity;
            //Matrix4 boneT = Matrix4.Identity;
            //Matrix4 boneR = Matrix4.Identity;
            //AnimationPose? animPose = GetAnimationPose(bone.Name);

            //if(animPose != null)
            //{
            //    if(animPose.AlreadyInterpolated)
            //    {
            //        var a = animPose.GetTranslationKeyFrame(animPose.FindTranslationKeyFrame(animTime)).Translation;
            //        boneT = Matrix4.CreateTranslation(a);
            //        var b = animPose.GetRotationKeyFrame(animPose.FindRotationKeyFrame(animTime)).Rotation;
            //        boneR = Matrix4.CreateFromQuaternion(b);
            //    }
            //    else
            //    {
            //        var a = animPose.GetInterpolatedTranslationKeyFrame(animTime);
            //        boneT = Matrix4.CreateTranslation(a);
            //        var b = animPose.GetInterpolatedRotationKeyFrame(animTime);
            //        boneR = Matrix4.CreateFromQuaternion(b);
            //        animPose.AlreadyInterpolated = true;
            //    }

            //    localAnim = boneT * boneR;

            //    if(bone.BoneIndex > -1)
            //    {
            //        //bone->finalTransform = bone->parent->finalTransform * local_anim;
            //        //d_animation_matrix[bone_i] = bone->finalTransform * bone_offset; //Change this to match OGL

            //        bone.FinalTransform = bone.FinalTransform * localAnim;
            //        Matrix4 final = bone.FinalTransform * bone.BoneOffset;

            //        //bone.FinalTransform = bone.Parent.FinalTransform * localAnim;
            //        //Matrix4 final = bone.FinalTransform * bone.BoneOffset;

            //        //Matrix4 final = bone.Parent.FinalTransform * localAnim;
            //        //final = final * bone.BoneOffset;

            //        //final = boneT * boneR * bone.BoneOffset;
            //        if (AnimationMatrices.ContainsKey(bone.Name))
            //            AnimationMatrices[bone.Name] = final;
            //        else
            //            AnimationMatrices.Add(bone.Name, final);
            //    }
            //}

            //foreach(var boneChild in bone.Children)
            //{
            //    AnimateKeyFrames(boneChild, animTime);
            //}
        }

        public Matrix4 GetAnimMatrixForBone(Bone bone, double animTime)
        {
            AnimationPose? animPose = GetAnimationPose(bone.Name);

            if (animPose != null)
            {
                Matrix4 localAnim = Matrix4.Identity;
                Matrix4 boneT;
                Matrix4 boneR;

                if (animPose.AlreadyInterpolated)
                {
                    var trans = animPose.GetTranslationKeyFrame(animPose.FindTranslationKeyFrame(animTime)).Translation;
                    boneT = Matrix4.CreateTranslation(trans);
                    var rot = animPose.GetRotationKeyFrame(animPose.FindRotationKeyFrame(animTime)).Rotation;
                    boneR = Matrix4.CreateFromQuaternion(rot);
                }
                else
                {
                    var trans = animPose.GetInterpolatedTranslationKeyFrame(animTime);
                    boneT = Matrix4.CreateTranslation(trans);
                    var rot = animPose.GetInterpolatedRotationKeyFrame(animTime);
                    boneR = Matrix4.CreateFromQuaternion(rot);
                    animPose.AlreadyInterpolated = true;
                }

                throw new NotImplementedException();
                //switch(Engine.editorData.animType)
                //{
                //    case (0):
                //        localAnim = boneR * boneT;
                //        break;
                //    case (1):
                //        localAnim = boneT * boneR;
                //        break;
                //    case (2):
                //        localAnim = Matrix4.Transpose(boneR) * Matrix4.Transpose(boneT);
                //        break;
                //    case (3):
                //        localAnim = Matrix4.Transpose(boneT) * Matrix4.Transpose(boneR);
                //        break;
                //    case (4):
                //        localAnim = Matrix4.Transpose(boneR) * boneT;
                //        break;
                //    case (5):
                //        localAnim = boneT * Matrix4.Transpose(boneR);
                //        break;
                //    case (6):
                //        localAnim = boneR * Matrix4.Transpose(boneT);
                //        break;
                //    case (7):
                //        localAnim = Matrix4.Transpose(boneT) * boneR;
                //        break;
                //}

                //if(Engine.editorData.animEndType == 0)
                //    return Matrix4.Transpose(localAnim);
                //else
                //    return localAnim;
            }

            return Matrix4.Identity;
        }

        public AnimationPose? GetAnimationPose(string boneName)
        {
            if(BoneMapping.ContainsKey(boneName))
                return BoneMapping[boneName];

            return null;
        }

        public void SetAnimationPose(string boneName, AnimationPose pose)
        {
            BoneMapping[boneName] = pose;
        }

        public void AddAnimationPose(string boneName, AnimationPose pose)
        {
            if(!BoneMapping.ContainsKey(boneName))
                BoneMapping.Add(boneName, pose);

            BoneMapping[boneName] = pose;
        }

        public double GetLocalTimer()
        {
            return LocalTimer;
        }

        public void Update(double delta)
        {
            LocalTimer += TimeStep(delta);
            if (LocalTimer > DurationInTicks)
                Reset();
        }

        public void Reset(double animationSpeed = -1)
        {
            LocalTimer = 0.0f;
            //BlendUpdateRatio = 1.0f;
        }

        public double TimeStep(double delta)
        {
            //return delta / 1000 * TicksPerSecond * BlendUpdateRatio;
            return delta*10;
        }

        public bool IsOver(double delta)
        {
            return LocalTimer + TimeStep(delta) >= DurationInTicks;
        }

    }
}
