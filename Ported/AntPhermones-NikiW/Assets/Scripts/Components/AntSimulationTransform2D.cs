﻿using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

// NWALKER: Change this to a local to world system.
[NoAlias]
public struct AntSimulationTransform2D : IComponentData
{
    [GhostField(Quantization = 1000)]
    public float2 position;
    [GhostField(Quantization = 360)]
    public float facingAngle;
}