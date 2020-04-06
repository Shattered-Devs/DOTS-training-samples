﻿using System;
using Unity.Entities;
using Unity.Mathematics;

[UpdateAfter(typeof(ArmSystem))]
public class FingerSystem : SystemBase
{
    private BeginSimulationEntityCommandBufferSystem beginSimECBSystem;
    protected override void OnCreate()
    {
        beginSimECBSystem = World.GetExistingSystem<BeginSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var ecb = beginSimECBSystem.CreateCommandBuffer().ToConcurrent();
        
        var UpBases = GetComponentDataFromEntity<ArmBasesUp>(true);
        var RightBases = GetComponentDataFromEntity<ArmBasisRight>(true);
        var ForwardBases = GetComponentDataFromEntity<ArmBasesForward>(true);

        var ArmJointsFromEntity = GetBufferFromEntity<ArmJointElementData>(true);
        var ArmGrabTs = GetComponentDataFromEntity<ArmGrabTimer>(true);
        var ArmRockRecords = GetComponentDataFromEntity<ArmLastRockRecord>(true);
        
        //float dt = Time.DeltaTime;
        float t = (float)Time.ElapsedTime;

        var grabCopyJob = Entities
            .WithNone<FingerGrabbedTag>()
            .WithReadOnly(ArmGrabTs)
            .ForEach((Entity entity,int entityInQueryIndex,
                ref FingerGrabTimer fingerGrabT,
            in FingerParent fingerParent) =>
        {
            Entity armParent = fingerParent.armParentEntity;
            float armGrabT = ArmGrabTs[armParent];
            fingerGrabT = armGrabT;

            if (fingerGrabT >= 1.0f)
            {
                ecb.AddComponent<FingerGrabbedTag>(entityInQueryIndex,entity);
            }
            
        }).ScheduleParallel(Dependency);
        
        var IKJob = Entities
            .WithReadOnly(ArmJointsFromEntity)
            .WithReadOnly(UpBases)
            .WithReadOnly(ForwardBases)
            .WithReadOnly(RightBases)
            .WithReadOnly(ArmRockRecords)
            .ForEach((
                ref DynamicBuffer<FingerJointElementData> fingerJoints,
                in FingerParent fingerParent,
                in FingerIndex fingerIndex,
                in FingerGrabTimer fingerGrabT,
                in FingerThickness fingerThickness) =>
            {
                Entity armParentEntity = fingerParent.armParentEntity;
                var armJointData = ArmJointsFromEntity[armParentEntity];
                var wristPos = armJointData[armJointData.Length - 1].value;
                var rockData = ArmRockRecords[armParentEntity];
                
                float3 armUp = UpBases[armParentEntity];
                float3 armForward = ForwardBases[armParentEntity];
                float3 armRight = RightBases[armParentEntity];
                
                var fingerOffsetX = -0.12f;
                var fingerSpacing = 0.08f;
                
                
               //get base targetPosition
                float3 fingerPos = wristPos + armRight * (fingerOffsetX + fingerIndex * fingerSpacing);
                float3 fingerTarget = fingerPos + armForward * (.5f - .1f * fingerGrabT);
                //finger wiggle
                fingerTarget += .2f * armUp * math.sin((t + fingerIndex * .2f) * 3f)  * (1f - fingerGrabT);
                
                float3 rockFingerDelta = fingerTarget - rockData.pos;
                float3 rockFingerPos = rockData.pos +
                                       math.normalize(rockFingerDelta)  * (rockData.size * .5f + fingerThickness);


                fingerTarget = math.lerp(fingerTarget, rockFingerPos, fingerGrabT);
                
                //todo add arm spread during throw here, before IK
                
                
                //todo add in variable bonelength for each finger in a component
                FABRIK.Solve(fingerJoints.AsNativeArray().Reinterpret<float3>(), 0.2f, fingerPos, fingerTarget,
                    0.1f * armUp);
                
            }).ScheduleParallel(grabCopyJob);
        
        var thumbIKJob = Entities
            .WithNone<FingerIndex>()
            .WithReadOnly(ArmJointsFromEntity)
            .WithReadOnly(UpBases)
            .WithReadOnly(ForwardBases)
            .WithReadOnly(RightBases)
            .WithReadOnly(ArmRockRecords)
            .ForEach((
                ref DynamicBuffer<FingerJointElementData> thumbJoints,
                in FingerParent thumbParent,
                in FingerGrabTimer grabT,
                in FingerThickness thickness) =>
            {
                Entity armParentEntity = thumbParent.armParentEntity;
                var armJointData = ArmJointsFromEntity[armParentEntity];
                var wristPos = armJointData[armJointData.Length - 1].value;
                var rockData = ArmRockRecords[armParentEntity];
                
                float3 armUp = UpBases[armParentEntity];
                float3 armForward = ForwardBases[armParentEntity];
                float3 armRight = RightBases[armParentEntity];
                
                var thumbOffsetX = -0.08f;
                
                //get base targetPosition
                float3 thumbPos = wristPos + armRight * thumbOffsetX;
                float3 thumbBendHint = -armRight - armForward * .5f;
                
               float3 thumbTarget = .1f * armRight * math.sin(t * 3f + .5f) * (1f - grabT);
               //thumb wiggle?
               thumbTarget += thumbPos - armRight * .15f + armForward * (.2f + .1f * grabT) - armUp * .1f;
               
               float3 rockThumbDelta = thumbTarget - rockData.pos;
               float3 rockThumbPos = rockData.pos +
                                      math.normalize(rockThumbDelta)  * (rockData.size * .5f + thickness);


               thumbTarget = math.lerp(thumbTarget, rockThumbPos, grabT);
               
                FABRIK.Solve(thumbJoints.AsNativeArray().Reinterpret<float3>(), 0.13f, thumbPos, thumbTarget,
                    0.1f * thumbBendHint);
            }).ScheduleParallel(IKJob);

       
        Dependency = thumbIKJob;
    }
}