using UnityEngine;
using UnityEditor;

public class gameSettingsMenu : EditorWindow
{

    [SerializeField] GameObject hexagonTile,bombTile,effect;
    [SerializeField] int scoreMultiplier = 5;
    [SerializeField] int bombScore = 1000;
    [SerializeField] int width = 8,height = 9;
    [SerializeField] float colOffset = 1.3f,rowOffset = 1.5f;
    [SerializeField] float cameraOffset = 1f;



    [SerializeField] Color[] tileColors;
    [SerializeField] int colorCount = 5;
  

    [SerializeField] bool firstSetting = true;

    string errorMessage = "";


    
  	[MenuItem("Game Settings/Settings")]
	public static void showWindow () {
		GetWindow<gameSettingsMenu>("Game Settings");
	}


    
    protected void OnEnable () {

        //first color assignment. Add more if needed, default maximum value is 10 color.
        if(firstSetting) {
            tileColors = new Color[]{Color.red,Color.green,Color.blue,
                        Color.white,Color.white,Color.white,Color.white,
                        Color.white,Color.white,Color.white};
            firstSetting = false;           
        }

        //read the last settings and assign them if exist.
        var data = EditorPrefs.GetString("gameSettingsMenu", JsonUtility.ToJson(this, false));
        JsonUtility.FromJsonOverwrite(data, this);
    }
 
    //save settings before exit.
    protected void OnDisable () {
        var data = JsonUtility.ToJson(this, false);
        EditorPrefs.SetString("gameSettingsMenu", data);
    }

    void OnGUI () {
        

        //General settings menu
        ////////////////////////
        GUILayout.Label("General Settings", EditorStyles.boldLabel);
        
        
        hexagonTile = (GameObject)EditorGUILayout.ObjectField("Hexagon Tile:",hexagonTile,typeof(GameObject),true);
        bombTile = (GameObject)EditorGUILayout.ObjectField("Bomb Tile:",bombTile,typeof(GameObject),true);
        effect = (GameObject)EditorGUILayout.ObjectField("Deleting Effect:",effect,typeof(GameObject),true);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Score Multiplier:\t");
        scoreMultiplier = EditorGUILayout.IntSlider(scoreMultiplier, 1, 20);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Bomb Score:\t");
        bombScore = EditorGUILayout.IntSlider(bombScore, 100, 5000);
        GUILayout.EndHorizontal();
      



        //Size settings menu
        ////////////////////////
        GUILayout.Space(20);
        GUILayout.Label("Size Settings", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Width:\t\t");
        width = EditorGUILayout.IntSlider(width, 3, 10);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Height:\t\t");
        height = EditorGUILayout.IntSlider(height, 3, 10);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Column Offset:\t");
        colOffset = EditorGUILayout.Slider(colOffset,0,50);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Row Offset:\t");
        rowOffset = EditorGUILayout.Slider(rowOffset,0,50);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Camera Size Offset:\t");
        cameraOffset = EditorGUILayout.Slider(cameraOffset,0,20);
        GUILayout.EndHorizontal();

    

        //Color settings menu
        ////////////////////////
        GUILayout.Space(20);
        GUILayout.Label("Color Settings", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Color Count:\t");
        //change max color count if needed, default is 10.
        colorCount = EditorGUILayout.IntSlider(colorCount, 3, 10);
        GUILayout.EndHorizontal();

        for(int num = 0; num < colorCount;num++) {
            tileColors[num] = EditorGUILayout.ColorField("Color " + (num+1), tileColors[num]);
        }
    

        
        //grid creation
        ////////////////////////
        if (GUILayout.Button("Create Grid")) {
            if(hexagonTile == null || bombTile == null || effect == null) {
                errorMessage = "Assign all tiles!";
            }
            else {
                errorMessage = "";
                createGrid();
            }
		}
        GUILayout.Label(errorMessage, EditorStyles.boldLabel);
 
	}

    void createGrid() {

        //delete if there is any grid object
        if(GameObject.Find("grid")) {
            DestroyImmediate(GameObject.Find("grid"));
        }

        //set camera position
        float xVal = ((width-1)*colOffset)/2f; 
        float yVal = ((height+3)*rowOffset)/2f; 
        //(height+3) for set the position higher from the center. default is height
        Vector3 cameraPos = new Vector3(xVal,yVal,0);
        Camera.main.transform.position = cameraPos;


        //create new parent as grid and add tiles to this parent object.
        GameObject parentObject = new GameObject("grid");
        //assign game system
        parentObject.AddComponent<gameSystem>().assignValues(hexagonTile,bombTile,effect,height,width,scoreMultiplier,bombScore,colorCount,tileColors,colOffset,rowOffset);
        for(int col = 0; col < width; col++) {
            for(int row = 0; row < height; row++) {
                Vector3 tilePos = new Vector3(col*colOffset,row*rowOffset,1);
                //set tile position higher every 2 tile.
                if(col%2 == 1) {
                    tilePos.y += rowOffset/2f;
                }
                GameObject clone = Instantiate(hexagonTile,tilePos,hexagonTile.transform.rotation);
                clone.name = col.ToString() + "-" + row.ToString(); //use column and row number as coordinate system
                clone.transform.SetParent(parentObject.transform);
                //assign hexTile to tile;
                clone.AddComponent<hexTile>().assingValues(tileColors,colorCount,colOffset,rowOffset,false);
  
            }
        }

        //set camera size
        fitToScreen(Camera.main,parentObject.transform,1,cameraOffset);




    }


    //set the size as the camera can see every child in parent
    //adding lastIncrement for prevent errors when screen ratio changed.
    void fitToScreen(Camera cam, Transform parent, int increment, float lastIncrement) {

        bool allVisible = false;
        cam.orthographicSize = 0.1f;    //zero not working

        while(!allVisible) {
            allVisible = true;
            foreach (Transform tile in parent) {
                Vector3 tilePos = cam.WorldToViewportPoint(tile.transform.position);
                //if there is any object out of the view, add increment to size and continue loop.
                if(tilePos.x < 0 || tilePos.x > 1 || tilePos.y < 0 || tilePos.y > 1) {
                    allVisible = false;
                    cam.orthographicSize += increment;
                    break;
                }
            }
        }

        cam.orthographicSize += lastIncrement;
    }



}
