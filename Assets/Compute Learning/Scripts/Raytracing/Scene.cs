using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scene", order = 0)]
public class Scene : ScriptableObject {
    public List<SceneMaterial> Materials = new List<SceneMaterial>();
    public List<SceneSphere> Spheres = new List<SceneSphere>();
}