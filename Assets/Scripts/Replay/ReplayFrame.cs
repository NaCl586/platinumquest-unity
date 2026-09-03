using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

[System.Serializable]
public class ReplayFrame
{
    //Positions
    public float pX,
        pY,
        pZ; //marble pos
    public float rX,
        rY,
        rZ,
        rW; //marble rotation
    public float sX,
        sY,
        sZ; //marble localScale
    public float cX,
        cY,
        cZ; //camera offset
    public float vX,
        vY,
        vZ; //marble velocity
    public float wX,
        wY,
        wZ; //marble angular velocity

    //bounce
    public int bounce;
    public float bounceStrength;
    public float bX,
        bY,
        bZ; // collision point
    public float nX,
        nY,
        nZ; // collision normal

    //Run data
    public string activePowerup; //current powerup (enum -> string)
    public float time; //game's time
    public float contactPct;
    public float slipAmount;
    public int jump; //when player presses jump
    public int gemCount; //total gem count
    public int respawn;

    //Gravity modifier
    public float gX,
        gY,
        gZ; //gravity direction
    public float gravityStrength; //gravity strength

    //Game State
    public int gameFinished;
    public int teleportFinished;

    // Appends an ObjectTransform to the end of a byte list.
    public void AppendToByteList(List<byte> byteList)
    {
        byteList.AddRange(System.BitConverter.GetBytes(pX));
        byteList.AddRange(System.BitConverter.GetBytes(pY));
        byteList.AddRange(System.BitConverter.GetBytes(pZ));

        byteList.AddRange(System.BitConverter.GetBytes(rX));
        byteList.AddRange(System.BitConverter.GetBytes(rY));
        byteList.AddRange(System.BitConverter.GetBytes(rZ));
        byteList.AddRange(System.BitConverter.GetBytes(rW));

        byteList.AddRange(System.BitConverter.GetBytes(sX));
        byteList.AddRange(System.BitConverter.GetBytes(sY));
        byteList.AddRange(System.BitConverter.GetBytes(sZ));

        byteList.AddRange(System.BitConverter.GetBytes(cX));
        byteList.AddRange(System.BitConverter.GetBytes(cY));
        byteList.AddRange(System.BitConverter.GetBytes(cZ));

        byteList.AddRange(System.BitConverter.GetBytes(vX));
        byteList.AddRange(System.BitConverter.GetBytes(vY));
        byteList.AddRange(System.BitConverter.GetBytes(vZ));

        byteList.AddRange(System.BitConverter.GetBytes(wX));
        byteList.AddRange(System.BitConverter.GetBytes(wY));
        byteList.AddRange(System.BitConverter.GetBytes(wZ));

        byteList.AddRange(System.BitConverter.GetBytes(bounce));

        byteList.AddRange(System.BitConverter.GetBytes(bounceStrength));

        byteList.AddRange(System.BitConverter.GetBytes(bX));
        byteList.AddRange(System.BitConverter.GetBytes(bY));
        byteList.AddRange(System.BitConverter.GetBytes(bZ));

        byteList.AddRange(System.BitConverter.GetBytes(nX));
        byteList.AddRange(System.BitConverter.GetBytes(nY));
        byteList.AddRange(System.BitConverter.GetBytes(nZ));

        byteList.AddRange(System.Text.Encoding.ASCII.GetBytes(activePowerup));
        byteList.Add(0); //'\0' in C equivalent

        byteList.AddRange(System.BitConverter.GetBytes(time));
        byteList.AddRange(System.BitConverter.GetBytes(contactPct));
        byteList.AddRange(System.BitConverter.GetBytes(slipAmount));
        byteList.AddRange(System.BitConverter.GetBytes(jump));
        byteList.AddRange(System.BitConverter.GetBytes(gemCount));
        byteList.AddRange(System.BitConverter.GetBytes(respawn));

        byteList.AddRange(System.BitConverter.GetBytes(gX));
        byteList.AddRange(System.BitConverter.GetBytes(gY));
        byteList.AddRange(System.BitConverter.GetBytes(gZ));

        byteList.AddRange(System.BitConverter.GetBytes(gravityStrength));

        byteList.AddRange(System.BitConverter.GetBytes(gameFinished));
        byteList.AddRange(System.BitConverter.GetBytes(teleportFinished));
    }

    // Gets an ObjectTransform from a byte list and returns the new position
    // Note: you'd think we would want to also pop it from the beginning. This would *probably* be fine,
    // but it could also be *very* slow. It's easier to just make a note to move the "playhead"
    public int GetFromByteArray(byte[] byteArray, int playhead)
    {
        pX = System.BitConverter.ToSingle(byteArray, playhead);
        playhead += 4;
        pY = System.BitConverter.ToSingle(byteArray, playhead);
        playhead += 4;
        pZ = System.BitConverter.ToSingle(byteArray, playhead);
        playhead += 4;

        rX = System.BitConverter.ToSingle(byteArray, playhead);
        playhead += 4;
        rY = System.BitConverter.ToSingle(byteArray, playhead);
        playhead += 4;
        rZ = System.BitConverter.ToSingle(byteArray, playhead);
        playhead += 4;
        rW = System.BitConverter.ToSingle(byteArray, playhead);
        playhead += 4;

        sX = System.BitConverter.ToSingle(byteArray, playhead);
        playhead += 4;
        sY = System.BitConverter.ToSingle(byteArray, playhead);
        playhead += 4;
        sZ = System.BitConverter.ToSingle(byteArray, playhead);
        playhead += 4;

        cX = System.BitConverter.ToSingle(byteArray, playhead);
        playhead += 4;
        cY = System.BitConverter.ToSingle(byteArray, playhead);
        playhead += 4;
        cZ = System.BitConverter.ToSingle(byteArray, playhead);
        playhead += 4;

        vX = System.BitConverter.ToSingle(byteArray, playhead);
        playhead += 4;
        vY = System.BitConverter.ToSingle(byteArray, playhead);
        playhead += 4;
        vZ = System.BitConverter.ToSingle(byteArray, playhead);
        playhead += 4;

        wX = System.BitConverter.ToSingle(byteArray, playhead);
        playhead += 4;
        wY = System.BitConverter.ToSingle(byteArray, playhead);
        playhead += 4;
        wZ = System.BitConverter.ToSingle(byteArray, playhead);
        playhead += 4;

        bounce = System.BitConverter.ToInt32(byteArray, playhead);
        playhead += 4;

        bounceStrength = System.BitConverter.ToSingle(byteArray, playhead);
        playhead += 4;

        bX = System.BitConverter.ToSingle(byteArray, playhead);
        playhead += 4;

        bY = System.BitConverter.ToSingle(byteArray, playhead);
        playhead += 4;

        bZ = System.BitConverter.ToSingle(byteArray, playhead);
        playhead += 4;

        nX = System.BitConverter.ToSingle(byteArray, playhead);
        playhead += 4;

        nY = System.BitConverter.ToSingle(byteArray, playhead);
        playhead += 4;

        nZ = System.BitConverter.ToSingle(byteArray, playhead);
        playhead += 4;

        int stringStart = playhead;
        while (byteArray[playhead] != 0)
            playhead++;

        activePowerup = System.Text.Encoding.ASCII.GetString(
            byteArray,
            stringStart,
            playhead - stringStart
        );
        playhead++;

        time = System.BitConverter.ToSingle(byteArray, playhead);
        playhead += 4;
        contactPct = System.BitConverter.ToSingle(byteArray, playhead);
        playhead += 4;
        slipAmount = System.BitConverter.ToSingle(byteArray, playhead);
        playhead += 4;
        jump = System.BitConverter.ToInt32(byteArray, playhead);
        playhead += 4;
        gemCount = System.BitConverter.ToInt32(byteArray, playhead);
        playhead += 4;
        respawn = System.BitConverter.ToInt32(byteArray, playhead);
        playhead += 4;

        gX = System.BitConverter.ToSingle(byteArray, playhead);
        playhead += 4;
        gY = System.BitConverter.ToSingle(byteArray, playhead);
        playhead += 4;
        gZ = System.BitConverter.ToSingle(byteArray, playhead);
        playhead += 4;

        gravityStrength = System.BitConverter.ToSingle(byteArray, playhead);
        playhead += 4;

        gameFinished = System.BitConverter.ToInt32(byteArray, playhead);
        playhead += 4;
        teleportFinished = System.BitConverter.ToInt32(byteArray, playhead);
        playhead += 4;

        return playhead;
    }

    public ReplayFrame() { }

    public ReplayFrame(
        Vector3 _position,
        Quaternion _rotation,
        Vector3 _localScale,
        Vector3 _camera,
        Vector3 _velocity,
        Vector3 _angularVelocity,
        int _bounce,
        float _bounceStrength,
        Vector3 _bouncePoint,
        Vector3 _bounceNormal,
        string _activePowerup,
        float _time,
        float _contactPct,
        float _slipAmount,
        int _jump,
        int _gemCount,
        int _respawn,
        Vector3 _gravityDirection,
        float _gravityStrength,
        int _gameFinished,
        int _teleportFinished
    )
    {
        pX = _position.x;
        pY = _position.y;
        pZ = _position.z;

        rX = _rotation.x;
        rY = _rotation.y;
        rZ = _rotation.z;
        rW = _rotation.w;

        sX = _localScale.x;
        sY = _localScale.y;
        sZ = _localScale.z;

        cX = _camera.x;
        cY = _camera.y;
        cZ = _camera.z;

        vX = _velocity.x;
        vY = _velocity.y;
        vZ = _velocity.z;

        wX = _angularVelocity.x;
        wY = _angularVelocity.y;
        wZ = _angularVelocity.z;

        bounce = _bounce;
        bounceStrength = _bounceStrength;

        bX = _bouncePoint.x;
        bY = _bouncePoint.y;
        bZ = _bouncePoint.z;

        nX = _bounceNormal.x;
        nY = _bounceNormal.y;
        nZ = _bounceNormal.z;

        activePowerup = _activePowerup;
        time = _time;
        contactPct = _contactPct;
        slipAmount = _slipAmount;
        jump = _jump;
        gemCount = _gemCount;
        respawn = _respawn;

        gX = _gravityDirection.x;
        gY = _gravityDirection.y;
        gZ = _gravityDirection.z;

        gravityStrength = _gravityStrength;

        gameFinished = _gameFinished;
        teleportFinished = _teleportFinished;
    }

    public Vector3 GetPosition() => new Vector3(pX, pY, pZ);

    public Quaternion GetRotation() => new Quaternion(rX, rY, rZ, rW);

    public Vector3 GetLocalScale() => new Vector3(sX, sY, sZ);

    public Vector3 GetCameraOffset() => new Vector3(cX, cY, cZ);

    public Vector3 GetVelocity() => new Vector3(vX, vY, vZ);

    public Vector3 GetAngularVelocity() => new Vector3(wX, wY, wZ);

    public Vector3 GetGravityDirection() => new Vector3(gX, gY, gZ);

    public float GetGravityStrength() => gravityStrength;

    public Vector3 GetBouncePoint() => new Vector3(bX, bY, bZ);

    public Vector3 GetBounceNormal() => new Vector3(nX, nY, nZ);
}
