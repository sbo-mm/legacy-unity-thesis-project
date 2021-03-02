using System.IO;
using System.Collections;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra.Factorization;
using MathNet.Numerics.LinearAlgebra;
using UnityEngine;
using ExtendedMathUtils;

public class RotatorCuff : MonoBehaviour
{

    // Start is called before the first frame update
    void Start()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        SM_Utils.AutoWeld(mesh, 1e-10f, 10f);

        int[] tris = mesh.triangles;
        Vector3[] verts = mesh.vertices;

        Debug.Log(tris.Length);
        Debug.Log(verts.Length);

        /*
        string vertsstr = string.Join(",", verts).Replace("(", "").Replace(")", "").Replace(" ", "");
        string trisstr = string.Join(",", tris).Replace(" ", "");

        string path = "/Users/SophusOlsen/Desktop/";

        using (StreamWriter sw = new StreamWriter(path + "vertices.txt", false))
        {
            sw.Write(vertsstr);
        }

        using (StreamWriter sw = new StreamWriter(path + "triangles.txt", false))
        {
            sw.Write(trisstr);
        }
        */

    }

    // Update is called once per frame
    void Update()
    {
    }
}
