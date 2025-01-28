using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using HarmonyGridSystem.Objects;

namespace HarmonyGridSystem.Builder
{
    public class ObjectNoter : MonoBehaviour
    {
        [SerializeField] LookUpTable lookupTable;
        [SerializeField] TextAsset jsonFile;

        ObjectCreator objectCreator;
        public Dictionary<string, ShapeData> shapes;

        int pageWidth;
        int pageHeight;

        // Start is called before the first frame update
        void Start()
        {
            objectCreator = GetComponent<ObjectCreator>();

            var rootList = JsonConvert.DeserializeObject<List<ShapeData>>(jsonFile.ToString());

            foreach (var item in rootList)
            {
                item.name = item.name.ToLower();
                item.newPositions = CreateVector(item.position);

                if (item.name == "original")
                {
                    pageWidth = Mathf.RoundToInt(item.width);
                    pageHeight = Mathf.RoundToInt(item.height);
                }
                else
                {
                    PlacedObjectSO objectSO = lookupTable.GetSO(item.text.ToLower());

                    //if (objectSO != null) objectCreator.PlaceObjectDown(objectSO, item.direction, Mathf.RoundToInt(item.newPositions.x), Mathf.RoundToInt(item.newPositions.y), pageWidth, pageHeight);
                    if (objectSO != null) objectCreator.PlaceObjectDown(objectSO, 0, Mathf.RoundToInt(item.newPositions.x), Mathf.RoundToInt(item.newPositions.y), pageWidth, pageHeight);
                }
            }
        }

        private Vector2 CreateVector(Pos oldPos)
        {
            Vector2 newPos = new Vector2();
            newPos.x = oldPos.x;
            newPos.y = oldPos.y;

            return newPos;
        }
    }

    public class ShapeData
    {
        public string name { get; set; }
        public string text { get; set; }
        public Pos position { get; set; }
        public float width { get; set; }
        public float height { get; set; }
        public float direction { get; set; }
        public Vector2 newPositions { get; set; }
    }

    public class Pos
    {
        public float x { get; set; }
        public float y { get; set; }
    }

    /*
     * 2550, 3300
    { '
        Shape0': { 'name': 'Rectangle0', 'text': 'table', 'position': (297, 583), 'width': 934, 'height': 215}, 
       'Shape1': { 'name': 'Rectangle1', 'text': 'cupboard', 'position': (1743, 298), 'width': 367, 'height': 496} 
    }
    */
}
