using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using MicroHawk.World;

namespace MicroHawk.Editor
{
    // Editor-only local authoring helpers. No runtime spawning or physical control.
    internal sealed class FacilityAuthoring
    {
        internal const string Root = "Assets/MicroHawk";
        internal readonly Dictionary<string, Material> Materials = new();
        internal readonly List<WorldEntity> Entities = new();
        internal readonly Transform Parent;

        internal FacilityAuthoring(Transform parent)
        {
            Parent = parent;
            AddMaterial("Concrete", new Color(.44f,.49f,.51f));
            AddMaterial("Pavement", new Color(.105f,.14f,.17f));
            AddMaterial("Steel", new Color(.22f,.29f,.33f), .65f);
            AddMaterial("Cladding", new Color(.54f,.62f,.65f), .4f);
            AddMaterial("Dark", new Color(.035f,.06f,.08f), .3f);
            AddMaterial("White", new Color(.84f,.89f,.87f));
            AddMaterial("Yellow", new Color(.98f,.61f,.07f));
            AddMaterial("Teal", new Color(.035f,.61f,.64f), .3f);
            AddMaterial("Orange", new Color(.98f,.29f,.045f));
            AddMaterial("Glass", new Color(.09f,.25f,.31f), .7f);
            AddMaterial("Wood", new Color(.43f,.29f,.16f));
            AddMaterial("Red", new Color(.8f,.05f,.025f));
        }

        private void AddMaterial(string name, Color color, float metal = 0)
        {
            var path = $"{Root}/Materials/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", metal);
            material.SetFloat("_Smoothness", metal > 0 ? .4f : .22f);
            Materials.Add(name, material);
            EditorUtility.SetDirty(material);
        }

        internal Transform Group(string name, Vector3 position, string id = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(Parent, false);
            go.transform.localPosition = position;
            if (id != null)
            {
                var entity = go.AddComponent<WorldEntity>();
                entity.ConfigureForAuthoring(id);
                Entities.Add(entity);
            }
            return go.transform;
        }

        internal GameObject Shape(Transform parent, string name, Vector3 pos, Vector3 scale,
            string material, PrimitiveType type = PrimitiveType.Cube, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Materials[material];
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            else if (type == PrimitiveType.Cylinder)
            {
                // Unity's cylinder primitive uses a capsule collider; static pads/tanks need flat mesh surfaces.
                Object.DestroyImmediate(go.GetComponent<Collider>());
                go.AddComponent<MeshCollider>().sharedMesh=go.GetComponent<MeshFilter>().sharedMesh;
            }
            return go;
        }

        internal void Beam(Transform parent, string name, Vector3 from, Vector3 to, float width, string material, bool collider = true)
        {
            var go = Shape(parent, name, (from + to)/2, new Vector3(width, (to-from).magnitude, width), material, PrimitiveType.Cube, collider);
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, to-from);
        }

        internal void Text(Transform parent, string name, string text, Vector3 pos, float size, Color color, bool floor = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            if (floor) go.transform.localRotation = Quaternion.Euler(90,0,0);
            var mesh = go.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mesh.fontSize = 80;
            mesh.characterSize = size / 8f;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = color;
            const string materialPath=Root+"/Materials/Signage.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(!material)
            {
                material=new Material(Shader.Find("MicroHawk/DepthTestedSignage"));
                AssetDatabase.CreateAsset(material,materialPath);
            }
            material.mainTexture=mesh.font.material.mainTexture;
            go.GetComponent<MeshRenderer>().sharedMaterial=material;
            go.AddComponent<MicroHawk.Presentation.SignageAtlas>();
        }

        internal void Sign(Transform parent, string text, Vector3 pos, float width = 6, float height = 1.1f)
        {
            Shape(parent, "SignPanel_" + text, pos, new Vector3(width,height,.14f), "Dark");
            Text(parent, "SignText_" + text, text, pos + new Vector3(0,0,-.085f), height*.65f, Color.white);
        }

        internal void Outline(Transform parent, Vector3 center, float width, float depth, string material = "Yellow")
        {
            Shape(parent,"Boundary_N",center+new Vector3(0,0,depth/2),new Vector3(width,.015f,.12f),material,PrimitiveType.Cube,false);
            Shape(parent,"Boundary_S",center+new Vector3(0,0,-depth/2),new Vector3(width,.015f,.12f),material,PrimitiveType.Cube,false);
            Shape(parent,"Boundary_E",center+new Vector3(width/2,0,0),new Vector3(.12f,.015f,depth),material,PrimitiveType.Cube,false);
            Shape(parent,"Boundary_W",center+new Vector3(-width/2,0,0),new Vector3(.12f,.015f,depth),material,PrimitiveType.Cube,false);
        }


        internal Transform Pad(string id, Vector3 position, string label, float diameter = 5)
        {
            var pad = Group(id, position, id);
            Shape(pad,"PadBase",new Vector3(0,.08f,0),new Vector3(diameter,.08f,diameter),"Steel",PrimitiveType.Cylinder);
            Shape(pad,"LandingSurface",new Vector3(0,.165f,0),new Vector3(diameter-.35f,.01f,diameter-.35f),"Teal",PrimitiveType.Cylinder);
            Text(pad,"LandingH","H",new Vector3(0,.182f,0),1.9f,Color.white,true);
            Text(pad,"PadId",label,new Vector3(0,.026f,-diameter/2-1),.58f,Color.white,true);
            Outline(pad,new Vector3(0,.025f,0),diameter+1,diameter+1,"White");
            return pad;
        }

        internal void Crate(Transform parent, Vector3 position)
        {
            Shape(parent,"Crate",position+Vector3.up*.6f,new Vector3(1.2f,1.2f,1.2f),"Wood");
            for(int i=-1;i<=1;i+=2)
                Shape(parent,"CrateBand",position+new Vector3(i*.38f,.6f,-.61f),new Vector3(.08f,1.2f,.025f),"Steel",PrimitiveType.Cube,false);
            Shape(parent,"Pallet",position+Vector3.up*.09f,new Vector3(1.4f,.18f,1.4f),"Wood");
        }

        internal void Barrier(Transform parent, Vector3 position, float length = 3)
        {
            Shape(parent,"Barrier",position+Vector3.up*.45f,new Vector3(length,.9f,.45f),"Concrete");
            Shape(parent,"SafetyStripe",position+new Vector3(0,.65f,-.235f),new Vector3(length,.22f,.015f),"Yellow",PrimitiveType.Cube,false);
            for(float x=-length/2+.25f;x<length/2;x+=.6f)
                Shape(parent,"Stripe",position+new Vector3(x,.65f,-.25f),new Vector3(.18f,.23f,.015f),"Dark",PrimitiveType.Cube,false);
        }
    }
}


