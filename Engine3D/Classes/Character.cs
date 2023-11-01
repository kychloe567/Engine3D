﻿using Cyotek.Drawing.BitmapFont;
using MagicPhysX;
using static MagicPhysX.NativeMethods;
using OpenTK.Audio.OpenAL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{

    public unsafe class Character
    {
        private float sensitivity = 180f;
        private float speed = 2f;
        //private float flySpeed = 10f;
        private float flySpeed = 2.5f;
        public bool isOnGround = false;
        private float gravity = 120;
        private float jumpForce = 20.0f;
        private float terminalVelocity = -0.7f;
        private float characterHeight = 4f;
        private float characterWidth = 2f;

        //PxCapsuleControllerDesc
        private float slopeLimit = 0.707f;
        private float contactOffset = 0.2f;
        private float stepOffset = 0.5f;
        private float density = 0.5f;

        private float StaticFriction = 0.5f;
        private float DynamicFriction = 0.5f;
        private float Restitution = 0.1f;

        private IntPtr capsuleControllerDescPtr;
        public PxCapsuleControllerDesc* GetCapsuleControllerDesc() { return (PxCapsuleControllerDesc*)capsuleControllerDescPtr.ToPointer(); }

        private IntPtr capsuleControllerPtr;
        public PxController* GetCapsuleController() { return (PxController*)capsuleControllerPtr.ToPointer(); }

        //----------------------------------------------
        private float thirdY = 10f;

        public bool noClip = true;

        private bool firstMove = true;
        public Vector2 lastPos;

        private Vector3 Velocity;
        public Vector3 Position;
        private Vector3 OrigPosition;

        public string PStr
        {
            get { return Math.Round(Position.X, 2).ToString() + "," + Math.Round(Position.Y, 2).ToString() + "," + Math.Round(Position.Z, 2).ToString(); }
        }

        public string VStr
        {
            get { return Math.Round(Velocity.X, 2).ToString() + "," + Math.Round(Velocity.Y, 2).ToString() + "," + Math.Round(Velocity.Z, 2).ToString(); }
        }

        public string LStr
        {
            get { return "Yaw: " + Math.Round(camera.GetYaw(), 2).ToString() + ", Pitch: " + Math.Round(camera.GetPitch(), 2).ToString(); }
        }

        private Physx physx;
        public WireframeMesh mesh;

        public Camera camera;


        public Character(WireframeMesh mesh, ref Physx physx, Vector3 position, Camera camera)
        {
            this.mesh = mesh;
            this.physx = physx;

            capsuleControllerDescPtr = new IntPtr(PxCapsuleControllerDesc_new_alloc());
            GetCapsuleControllerDesc()->height = characterHeight;
            GetCapsuleControllerDesc()->radius = characterWidth;
            GetCapsuleControllerDesc()->position = new PxExtendedVec3() { x = position.X, y = position.Y, z = position.Z };
            GetCapsuleControllerDesc()->upDirection = new PxVec3() { x = 0, y = 1, z = 0 };
            GetCapsuleControllerDesc()->slopeLimit = slopeLimit;
            GetCapsuleControllerDesc()->invisibleWallHeight = 0.0f;
            GetCapsuleControllerDesc()->contactOffset = contactOffset;
            GetCapsuleControllerDesc()->stepOffset = stepOffset;
            GetCapsuleControllerDesc()->density = density;
            GetCapsuleControllerDesc()->scaleCoeff = 1.0f;
            GetCapsuleControllerDesc()->material = physx.GetPhysics()->CreateMaterialMut(StaticFriction, DynamicFriction, Restitution);

            if (!PxCapsuleControllerDesc_isValid(GetCapsuleControllerDesc()))
                throw new Exception("Capsule Controller Descriptor is not valid!");

            capsuleControllerPtr = new IntPtr(physx.GetControllerManager()->CreateControllerMut((PxControllerDesc*)GetCapsuleControllerDesc()));

            //((PxRigidBody*)GetCapsuleController()->GetActor())->SetRigidBodyFlagMut(PxRigidBodyFlag.EnableCcd, true);

            mesh.lines = GetBoundLines();

            Velocity = Vector3.Zero;
            Position = position;
            OrigPosition = position;

            this.camera = camera;
            camera.SetPosition(position);
        }

        public void CalculateVelocity(KeyboardState keyboardState, MouseState mouseState, FrameEventArgs args)
        {
            if(keyboardState.IsKeyReleased(Keys.N))
            {
                noClip = !noClip;
                Velocity = Vector3.Zero;
                if(!noClip)
                {
                    PxExtendedVec3 vec3 = new PxExtendedVec3() { x = Position.X, y = Position.Y, z = Position.Z };
                    GetCapsuleController()->SetPositionMut(&vec3);
                }
            }

            float speed_ = speed;
            float flySpeed_ = flySpeed;
            if (keyboardState.IsKeyDown(Keys.LeftShift))
            {
                speed_ *= 2;
                flySpeed_ *= 2;
            }

            if (!noClip)
            {
                if (!isOnGround)
                    Velocity.Y = Velocity.Y - gravity * (float)Math.Pow(args.Time, 2) / 2;
                if (Velocity.Y < terminalVelocity)
                    Velocity.Y = terminalVelocity;
            }


            if (!noClip)
            {
                if (keyboardState.IsKeyDown(Keys.Space) && isOnGround)
                {
                    if (!noClip)
                        Velocity.Y += jumpForce * (float)args.Time;
                }
            }
            else
            {
                if (keyboardState.IsKeyDown(Keys.Space))
                {
                    Velocity.Y += flySpeed_ * 10 * (float)args.Time;
                }
            }

            if (keyboardState.IsKeyDown(Keys.LeftControl))
            {
                if (noClip)
                    Velocity.Y -= flySpeed_ * 10 * (float)args.Time;
            }

            if (keyboardState.IsKeyDown(Keys.Enter) || keyboardState.IsKeyDown(Keys.KeyPadEnter))
            {
                Position = OrigPosition;
                Velocity = Vector3.Zero;

                PxExtendedVec3 vec3 = new PxExtendedVec3() { x = Position.X, y = Position.Y, z = Position.Z };
                GetCapsuleController()->SetPositionMut(&vec3);
            }


            if (keyboardState.IsKeyDown(Keys.W))
            {
                if (!noClip)
                    Velocity += (camera.frontClamped * speed_) * (float)args.Time;
                else
                    Velocity += (camera.frontClamped * flySpeed_) * (float)args.Time;
            }
            if (keyboardState.IsKeyDown(Keys.S))
            {
                if (!noClip)
                    Velocity -= (camera.frontClamped * speed_) * (float)args.Time;
                else
                    Velocity -= (camera.frontClamped * flySpeed_) * (float)args.Time;
            }
            if (keyboardState.IsKeyDown(Keys.A))
            {
                if (!noClip)
                    Velocity -= (camera.right * speed_) * (float)args.Time;
                else
                    Velocity -= (camera.right * flySpeed_) * (float)args.Time;
            }
            if (keyboardState.IsKeyDown(Keys.D))
            {
                if (!noClip)
                    Velocity += (camera.right * speed_) * (float)args.Time;
                else
                    Velocity += (camera.right * flySpeed_) * (float)args.Time;
            }
        }

        public void UpdatePosition(KeyboardState keyboardState, MouseState mouseState, FrameEventArgs args)
        {
            Vector3 newPos = new Vector3();
            if (noClip)
            {
                newPos = Position + Velocity;
            }
            else
            {
                PxVec3 disp = new PxVec3() { x = Velocity.X, y = Velocity.Y, z = Velocity.Z };
                PxFilterData filterData = PxFilterData_new(PxEMPTY.PxEmpty);
                PxControllerFilters filter = PxControllerFilters_new(&filterData, null, null);
                PxControllerCollisionFlags result = GetCapsuleController()->MoveMut(&disp, 0.001f, (float)args.Time, &filter, null);
                isOnGround = (result & PxControllerCollisionFlags.CollisionDown) == PxControllerCollisionFlags.CollisionDown;
                if (isOnGround)
                {
                    Velocity.Y = 0;
                }
                if ((result & PxControllerCollisionFlags.CollisionUp) == PxControllerCollisionFlags.CollisionUp)
                {
                    Velocity.Y = 0;
                }
                PxExtendedVec3* pxPos = GetCapsuleController()->GetPosition();
                newPos = new Vector3((float)pxPos->x, (float)pxPos->y, (float)pxPos->z);
            }

            mesh.Position = newPos;
            Position = newPos;
        }

        public void AfterUpdate(MouseState mouseState, FrameEventArgs args, GameState gameRunning)
        {
            Velocity.X *= 0.9f;
            Velocity.Z *= 0.9f;

            if (noClip)
                Velocity.Y *= 0.1f;

            ZeroSmallVelocity();
            thirdY -= mouseState.ScrollDelta.Y;

            camera.SetPosition(Position);
            //camera.position.X -= (float)Math.Cos(MathHelper.DegreesToRadians(camera.yaw)) * thirdY;//-6.97959471
            //camera.position.Y += thirdY;
            //camera.position.Z -= (float)Math.Sin(MathHelper.DegreesToRadians(camera.yaw)) * thirdY;//-7.161373

            //thirdY = 0;
            //camera.position.X = Position.X;
            //camera.position.Y = Position.Y + characterHeight;
            //camera.position.Z = Position.Z;

            if (firstMove)
            {
                lastPos = new Vector2(mouseState.X, mouseState.Y);
                firstMove = false;
            }
            else
            {
                float deltaX = mouseState.X - lastPos.X;
                float deltaY = mouseState.Y - lastPos.Y;

                if (deltaX != 0 || deltaY != 0)
                {
                    lastPos = new Vector2(mouseState.X, mouseState.Y);

                    if (gameRunning == GameState.Running)
                    {
                        camera.SetYaw(camera.GetYaw() + deltaX * sensitivity * (float)args.Time);//45.73648
                        camera.SetPitch(camera.GetPitch() - deltaY * sensitivity * (float)args.Time);//-18.75002
                    }
                }
            }
        }


        public List<Line> GetBoundLines()
        {
            Capsule c = new Capsule(characterWidth, characterHeight*2, new Vector3(0, -(characterWidth+characterWidth), 0));
            List<Line> lines = c.GetWireframe(10);

            return lines;
        }

        private void ZeroSmallVelocity()
        {
            if (Velocity.X < 0.0001f && Velocity.X > -0.0001f)
                Velocity.X = 0;

            if (Velocity.Y < 0.0001f && Velocity.Y > -0.0001f)
                Velocity.Y = 0;

            if (Velocity.Z < 0.0001f && Velocity.Z > -0.0001f)
                Velocity.Z = 0;
        }
    }
}
