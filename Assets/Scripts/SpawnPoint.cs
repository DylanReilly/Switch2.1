using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    public static bool isUsed = false;

    public bool GetIsUsed()
    {
        return isUsed;
    }

    public void SetIsUsed(bool used)
    {
        isUsed = used;
    }

    public static SpawnPoint Deserialize(byte[] data)
    {
        SpawnPoint result = new SpawnPoint();
        using (MemoryStream m = new MemoryStream(data))
        {
            using (BinaryReader reader = new BinaryReader(m))
            {
                result.SetIsUsed(reader.ReadBoolean());
            }
        }
        return result;
    }

    public static byte[] Serialize(object obj)
    {
        var spawnPoint  = (SpawnPoint)obj;
        using (MemoryStream m = new MemoryStream())
        {
            using (BinaryWriter writer = new BinaryWriter(m))
            {
                writer.Write(spawnPoint.GetIsUsed());
            }
            return m.ToArray();
        }
    }
}
