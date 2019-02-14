﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingMapPoint : MapPoint {
    public Vector2 moveVector { get; protected set; }

    public MovingMapPoint(float i_angle, float i_height, byte ring, MapMarkerType mtype) : base(i_angle, i_height, ring, mtype)
    {
        moveVector = Vector2.zero;
    }

    override public bool DestroyRequest()
    {
        return false;
    }
}
