using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class gameSystem : MonoBehaviour
{
    public GameObject hexTile,bombTile,deleteEffect;

    GameObject[] lastSelectedTiles;

    GameObject pivotObject;

    public int gridHeight = 9;
    public int gridWidth = 8;

    public float colOffset,rowOffset;

    public int scoreMultiplier = 5;
    int currentScore  =0;
    public int bombScore;
    int bombCounter;

    public Color[] tileColors;
    public int colorCount;

    RuntimePlatform platform;

    bool isRotating = false;
    bool isSelectable = true;
    bool isSelected = false;
    bool isEnded = false;

    menuSystem gameUI;

    int[,] gridColors; 
    public string gridColorsString = "";

    AudioSource click,explode;
   
 

    //assign default values, it calls from gameSettingsMenu.cs
    public void assignValues(GameObject tile, GameObject bomb,GameObject effect,int height,int width,int score,int bombScr,int colorCnt,Color[] colors,float col, float row) {
        hexTile = tile;
        bombTile = bomb;
        deleteEffect = effect;
        
        gridHeight = height;
        gridWidth = width;
        
        scoreMultiplier = score;
        bombScore = bombScr;

        tileColors = colors;
        colorCount = colorCnt;

        colOffset = col;
        rowOffset = row;

    }

    void Start() {
        gridColors = new int[gridWidth,gridHeight];

        click = GameObject.Find("sounds/click").GetComponent<AudioSource>();
        explode = GameObject.Find("sounds/explode").GetComponent<AudioSource>();
        gameUI = GameObject.Find("Canvas").GetComponent<menuSystem>();
        pivotObject = GameObject.Find("pivot");

        //if user exit from last game, gameOver variable will be 0
        //if last game was ended(bomb, no more move), it will be 1
        int lastGameStatus = PlayerPrefs.GetInt("gameOver", 1);
        if(lastGameStatus == 1) {
            randomColorizeGrid();
        }
        else {
            loadLastGrid();
        }
        
        PlayerPrefs.SetInt("gameOver", 0);
        platform = Application.platform;

        
    }

    void Update() {
        if(!isEnded) {
            if(platform == RuntimePlatform.Android) {
                mobileControls();
            }
            else {
                editorControls();
            }
        }
    }

    void mobileControls() {
        if (Input.touchCount > 0 ) {
            if(Input.GetTouch(0).phase == TouchPhase.Ended && !isRotating && isSelectable) {
                selectTilesForRotation(Input.GetTouch(0).position);
            }

            //rotate if last rotation completed (!isRotating) and there is any selected tile (isSelected)
            else if(Input.GetTouch(0).phase == TouchPhase.Moved && !isRotating && isSelected){
                Vector2 rotationPoint = getMeanPosition(lastSelectedTiles);
                Vector2 touchPosOnWorld = Camera.main.ScreenToWorldPoint(Input.GetTouch(0).position);

                //get swipe direction and touch position relative to selected tiles.
                //for example if swipe direction is downward (deltaPosition.y < 0) and touch position is
                //on the left of the rotationPoint (touchPosOnWorld.x < rotationPoint.x), rotate tiles in
                //counter clockwise (10 means rotate in 10 speed on counter clockwise, -10 is same but rotate clockwise)
                if(Input.GetTouch(0).deltaPosition.y < 0) {
                    if(touchPosOnWorld.x < rotationPoint.x) {
                        startRotation(120,10,0);
                    }
                    else {
                        startRotation(120,-10,0);
                    }
                }
                else {
                    if(touchPosOnWorld.x < rotationPoint.x) {
                        startRotation(120,-10,0);
                    }
                    else {
                        startRotation(120,10,0);
                    }
                }
            }
        }
    }

    //On editor window, select tile with mouse and rotate them with up-down arrows
    void editorControls() {
        if (Input.GetMouseButtonDown(0) && !isRotating && isSelectable) {
            selectTilesForRotation(Input.mousePosition);
        }
        else if(Input.GetKeyUp(KeyCode.UpArrow) && !isRotating && isSelected) {
            startRotation(120,10,0); 
        }
        else if(Input.GetKeyUp(KeyCode.DownArrow) && !isRotating && isSelected) {
            startRotation(120,-10,0); 
        }
    }


    void selectTilesForRotation(Vector2 inputPos) {
        //8 is the bomb layer, raycast will detect layers expect 8
        int layerMask = 1 << 8;
        layerMask = ~layerMask;
        RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint((inputPos)), Vector2.zero,Mathf.Infinity,layerMask);
        if (hit.collider != null ) {
            GameObject[] surroundingTiles = hit.transform.parent.GetComponent<hexTile>().getTiles(hit.point);

            if(surroundingTiles != null) {
                //unselect lastSelectedTiles if they are assigned
                if(lastSelectedTiles != null) {
                    foreach(GameObject tile in lastSelectedTiles) {
                        if(tile != null) {
                            tile.GetComponent<hexTile>().tileOutline(false);
                        }
                    }
                }
  
                //select new tiles
                foreach(GameObject tile in surroundingTiles) {
                    if(tile != null) {
                        tile.GetComponent<hexTile>().tileOutline(true);
                    }
                    
                }

                lastSelectedTiles = surroundingTiles;     
                isSelected = true;

                //detect pivot point
                Vector3 rotationPoint = getMeanPosition(lastSelectedTiles);
                rotationPoint.z = 1;    //cant see on camera when it is 0
                pivotObject.transform.position = rotationPoint;
            }
        }
    }


    void startRotation(int angle,int speed,int step) {
        click.Play();           //sound effect

        //disable new tile selection or rotation
        isRotating = true;
        isSelectable = false;
        
        //scale down the selected tiles and disable the outlines of them
        foreach(GameObject tile in lastSelectedTiles) {
            if(tile != null) {
                tile.GetComponent<hexTile>().resizeTile(-1);
                tile.GetComponent<hexTile>().tileOutline(false);
            }
        }

        rotateTiles(angle,speed,step);
    }

    //it is a recursive function that rotate tiles until angle value reach 0,
    //it rotate speed and rotate direction determined by speed value
    //negative - rotate clockwise, positive - rotate counter clockwise
    //when angle reach 0, increase step value, when it reach 3, it completes one round
    //and loop will end
    void rotateTiles(int angle,int speed,int step) {
        if(angle > 0) {
            //rotate tiles around this point
            Vector2 rotationPoint = getMeanPosition(lastSelectedTiles);

            foreach(GameObject tile in lastSelectedTiles) {
                tile.transform.RotateAround(rotationPoint,Vector3.forward,speed);
            }
            //this function waits and call this rotateTiles function again
            StartCoroutine(waitForRotate(angle,speed,step));
        }
        else { 
            //if angle reach 0, it means it completed 1 step.
            //change the names according to rotation direction
            //if direction is +, shift the names backward on array. (1,2,3) -> (2,3,1)
            if(speed > 0) {
                string firstName = lastSelectedTiles[0].name;
                for(int tileNum = 0; tileNum < lastSelectedTiles.Length;tileNum++) {
                    if(tileNum == (lastSelectedTiles.Length - 1)) {
                        lastSelectedTiles[tileNum].name = firstName;
                    }
                    else {
                        lastSelectedTiles[tileNum].name = lastSelectedTiles[tileNum+1].name;
                    }
                }
            }
            //if direction is negative, shift forward (1,2,3) -> (3,1,2)
            else {
                string firstName = lastSelectedTiles[lastSelectedTiles.Length-1].name;
                for(int tileNum = lastSelectedTiles.Length-1; tileNum >= 0;tileNum--) {
                    if(tileNum == 0) {
                        lastSelectedTiles[tileNum].name = firstName;
                    }
                    else {
                        lastSelectedTiles[tileNum].name = lastSelectedTiles[tileNum-1].name;
                    }
                }
            }
            
            //if there is not any color match and step smaller than 2 (not completed full round)
            //continue rotating
            if(!colorCheck(lastSelectedTiles) && step < 2) {
                click.Play();                   //sound effect
                rotateTiles(120,speed,step+1);  //step+1 on every recall
            }
            else {
                if(step < 2) {
                    //if detect any match, step will be smaller than 3 (if it is 3, tiles turn back to starting position)
                    bombCountDown();    
                }

                //set pivot position to a place that camera cant see.
                pivotObject.transform.position = new Vector3(1000,1000,1);
                
                //some tiles matched and deleted, fill the empty grid with new tiles
                addNewTiles();

                //scale up rotated tiles
                foreach(GameObject tile in lastSelectedTiles) {
                    tile.GetComponent<hexTile>().resizeTile(1);
                }

                //enable new tile selection
                isSelected = false;
                isRotating = false;
            }
        }
    }

    IEnumerator waitForRotate(int angle,int speed,int step) {
        yield return new WaitForFixedUpdate();
        //speed can be negative, so get absolute value of speed.
        rotateTiles(angle -Mathf.Abs(speed),speed,step);
    }

    //get average position of the given objects
    Vector3 getMeanPosition(GameObject[] tiles) {
        Vector3 rotationPoint = Vector3.zero;
        
        foreach(GameObject tile in tiles) {
            rotationPoint += tile.transform.position;
        }
        
        rotationPoint /= lastSelectedTiles.Length;
        return rotationPoint;
    } 

    //bomb countdown check
    void bombCountDown() {
        //get all bombs with their tag
        GameObject[] bombs = GameObject.FindGameObjectsWithTag(bombTile.tag);

        foreach(GameObject bombObject in bombs) {
            //default bombTile object dont have hexTile code
            if(bombObject.GetComponent<hexTile>()) {
                //add -1 to remaining time of the bomb and assign it to remainingTime value
                int remainingTime = bombObject.GetComponent<hexTile>().setBombTimer(-1);
                if(remainingTime < 1) {
                    //if it is smaller than 1, finish the game.
                    //true value shows that the reason of ending is bomb. false for no more move.
                    gameUI.endGame(true);
                }
            }
        }
    }


    void saveLastGrid() {
        //assign tile colors to gridColors array
        foreach (Transform tiles in transform) { 
            int xCoord = int.Parse(tiles.name.Split('-')[0]);
            int yCoord = int.Parse(tiles.name.Split('-')[1]);

            hexTile tileCode = tiles.GetComponent<hexTile>();

            gridColors[xCoord,yCoord] = tileCode.getColor();
        }

        //save gridColors array to string for saving to PlayerPrefs
        gridColorsString = "";
        for(int x = 0; x < gridWidth; x++) {
            for(int y = 0; y < gridHeight; y++) {
                //add ',' for splitting when load this array
                gridColorsString += gridColors[x,y].ToString() + ",";
            } 
        }  

        //save them to PlayerPrefs
        PlayerPrefs.SetString("lastGrid", gridColorsString);
        PlayerPrefs.SetInt("lastScore", currentScore);
        PlayerPrefs.SetInt("lastBomb", bombCounter);
    }

    void loadLastGrid() {
        List<GameObject> newObjects = new List<GameObject>();
        List<GameObject> deletingObjects = new List<GameObject>();

        //load last grid data
        gridColorsString = PlayerPrefs.GetString("lastGrid", "");
        currentScore = PlayerPrefs.GetInt("lastScore",0);
        bombCounter = PlayerPrefs.GetInt("lastBomb",0);

        //assign loaded score
        gameUI.assingScore(currentScore);

        //convert gridColorsString value to int array and assign it to gridColors
        for(int x = 0; x < gridWidth; x++) {
            for(int y = 0; y < gridHeight; y++) {
                string colorValue = gridColorsString.Split(',')[x * gridHeight + y];
                gridColors[x,y] = int.Parse(colorValue);
            } 
        }

        //assign new colors to grid. get all tiles on grid
        foreach (Transform tiles in transform) {
                int xCoord = int.Parse(tiles.name.Split('-')[0]);
                int yCoord = int.Parse(tiles.name.Split('-')[1]);

                int colorNum =  gridColors[xCoord,yCoord];

                //in bomb tiles, the color number is colorNum + (10 * timer)
                if(colorNum >= 10) {
                    //calculate bomb timer with this (colorNum + (10 * timer)) equation
                    int bombTime = (colorNum - (colorNum % 10)) / 10;

                    //create new bomb tile on this position                    
                    GameObject clone = Instantiate(bombTile,tiles.position,bombTile.transform.rotation);
                    //change the name of the standart tile to "deleted"
                    string tmpName = tiles.name;
                    tiles.name = "deleted";

                    //assing its name to new bomb tile
                    clone.name = tmpName;
                    //game objects on this list will be deleted
                    deletingObjects.Add(tiles.gameObject);

                    //new tiles doesnt have a parent, it will set grid as parent to game objects on this list
                    newObjects.Add(clone);

                    //assign standart values
                    clone.AddComponent<hexTile>().assingValues(tileColors,colorCount,colOffset,rowOffset,true);
                    clone.GetComponent<hexTile>().setRandomColor(colorNum); //assign calculated color
                    clone.GetComponent<hexTile>().setBombTimer(bombTime);   //assign calculated timer
                }
                else {
                    //if it is normal tile, just assing color.
                    tiles.GetComponent<hexTile>().setRandomColor(colorNum);
                }
        }

        //destroy tiles that replaced with bomb
        foreach(GameObject tile in deletingObjects) {
            Destroy(tile);
        }

        //assign parent to bombs
        foreach(GameObject tile in newObjects) {
            tile.transform.parent = transform;
        }
    }


    void randomColorizeGrid() {
        foreach (Transform tiles in transform) {
            //this assing random color and return it
            int colorNum = tiles.GetComponent<hexTile>().setColor();

            //get coordinates from its name (2,3 ; 4,7 ..)
            int xCoord = int.Parse(tiles.name.Split('-')[0]);
            int yCoord = int.Parse(tiles.name.Split('-')[1]);

            //assign returned color
            gridColors[xCoord,yCoord] = colorNum;
        }
    }

    //detect color match
    bool colorCheck(GameObject[] tiles) {
        //it will true if there is any match
        bool returnValue = false;
        List<GameObject> deletingTiles = new List<GameObject>();

        //check sended tiles
        foreach(GameObject tile in tiles) {
            //this function chect that if any match on around tile, and send matched tiles
            //assing them to this list for deleting
            deletingTiles.AddRange(tile.GetComponent<hexTile>().checkColorsAround());
        }

        //unselect remaining tiles
        if(deletingTiles.Count > 0) {
            returnValue = true;

            foreach(GameObject tile in tiles) {
                if(tile != null) {
                    tile.GetComponent<hexTile>().tileOutline(false);
                }
            }
        }

        foreach(GameObject tile in deletingTiles) {
            //create delete effect clone, assign the color of tile and destroy tile
            GameObject cloneEffect = Instantiate(deleteEffect,tile.transform.position,Quaternion.identity);
            ParticleSystem effectParticle = cloneEffect.GetComponent<ParticleSystem>();
            ParticleSystem.MainModule particleMain = effectParticle.main;
            particleMain.startColor = tileColors[tile.GetComponent<hexTile>().getColor() % 10];
            effectParticle.Play(); 
            Destroy(tile);
            explode.Play();     //sound effect

            //add score
            currentScore += scoreMultiplier;
            gameUI.addScore(scoreMultiplier);

        }

    return returnValue;
    }

    void addNewTiles() {
        //cant detect deleted tiles instant, add some delay
        StartCoroutine(waitForAdding());
    }

    IEnumerator waitForAdding() {
        yield return new WaitForSeconds(0.01f);

        //drop tiles to empty places
        for(int col = 0; col < gridWidth; col++) {
            for(int row = 0; row < gridHeight; row++) {
                string tileName = col.ToString() + "-" + row.ToString();
                GameObject tile = GameObject.Find(tileName);
                if(tile != null) {
                    //it check if tile have empy places on its bottom
                    tile.GetComponent<hexTile>().dropControll();
                }
            }
            yield return new WaitForSeconds(0.01f);
        }

        //adding new tiles
        //random value for giving to new tiles
        int counter = 20;
        for(int col = 0; col < gridWidth; col++) {
            for(int row = 0; row < gridHeight; row++) {
                string tileName = col.ToString() + "-" + row.ToString();
                GameObject tile = GameObject.Find(tileName);
                if(tile == null) {
                    yield return new WaitForSeconds(0.03f);

                    //if it reach multiples of bombScore, add new bomb tile
                    //bombCounter prevent adding more than 1 bomb every multiple of bombScore.
                    if((currentScore -(currentScore % bombScore)) / bombScore > bombCounter) {
                        bombCounter++;

                        //setting position, y value is random with counter value (20,21,22..)
                        Vector3 newPos = new Vector3(col * colOffset,counter,1);
                        GameObject clone = Instantiate(bombTile,newPos,bombTile.transform.rotation);

                        //assign name and make child of grid (transform)
                        clone.name = col.ToString() + "-" + counter.ToString();
                        clone.transform.parent = transform;

                        //set default values, assign random time and random color (-1 means assign random)
                        clone.AddComponent<hexTile>().assingValues(tileColors,colorCount,colOffset,rowOffset,true);
                        clone.GetComponent<hexTile>().setBombTimer(Random.Range(5,9));
                        clone.GetComponent<hexTile>().setRandomColor(-1);
                        counter++;
                    }
                    else {
                        //if it is a normal tile, make same process without setting bomb timer
                        Vector3 newPos = new Vector3(col * colOffset,counter,1);
                        GameObject clone = Instantiate(hexTile,newPos,hexTile.transform.rotation);
                        clone.name = col.ToString() + "-" + counter.ToString();
                        clone.transform.parent = transform;
                        clone.AddComponent<hexTile>().assingValues(tileColors,colorCount,colOffset,rowOffset,false);
                        clone.GetComponent<hexTile>().setRandomColor(-1);
                        counter++;
                    }
                }
            }
        }

        //waiting for dropping tiles
        yield return new WaitForSeconds(0.15f);
        
        //check for new tiles that they have any match
        List<GameObject> controllList = new List<GameObject>();
        foreach(Transform tile in transform) {
            controllList.Add(tile.gameObject);
        }
        //if they have matches, add new tiles again
        if(colorCheck(controllList.ToArray())) {
            addNewTiles();
        }
        else {
            //if new tiles dont have a match, save this grid, and check for possible moves
            saveLastGrid();
            if(possibleMoveCheck()) {
                isSelectable = true;
            }
            else {
                //if there is not any possible move, finish the game
                gameUI.endGame(false);
            }
        }
    }



    bool possibleMoveCheck() {
        for(int x  = 1; x < gridWidth-1; x++) {
            for(int y  = 1; y < gridHeight-1; y++) {
                if(checkColorsAround(x,y) > 1) {
                    //if there is any possible move, return true value
                    return true;
                }
            }
        }
        return false;
    }

    //this checks matches on the gridColors array for perfomance increase.
    //it controlls tile on the (posX , posY) and around tiles for tile count with same color.
    //Because of the grid structure, adjacent tile coordinates changes,
    //for example, if x position of a tile is odd number, the position of the tile on the top right  
    //of this tile is (posX + 1, posY + 1), but if x position is an even number, position of
    //top rigth tile is (posX + 1, posY)
    //this function get all adjacent tile colors, compare with center tile and return same color count
    int checkColorsAround(int posX, int posY) {
        int tileColor = gridColors[posX,posY] % 10;
        int totalColor = 0;

        if(posX % 2 == 0) {
            if(gridColors[posX,posY - 1] % 10 == tileColor) {
                totalColor++;
            }
            
            if(gridColors[posX,posY + 1] % 10 == tileColor) {
                totalColor++;
            }
          
            if(gridColors[posX + 1,posY - 1] % 10 == tileColor) {
                totalColor++;
            }
            
            if(gridColors[posX + 1,posY] % 10 == tileColor) {
                totalColor++;
            }

            if(gridColors[posX - 1,posY - 1] % 10 == tileColor) {
                totalColor++;
            }
            
            if(gridColors[posX - 1,posY] % 10 == tileColor) {
                totalColor++;
            }
        }
        else {
            if(gridColors[posX,posY - 1] % 10 == tileColor) {
                totalColor++;
            }
            
            if(gridColors[posX,posY + 1] % 10 == tileColor) {
                totalColor++;
            }
          
            if(gridColors[posX + 1,posY] % 10 == tileColor) {
                totalColor++;
            }
            
            if(gridColors[posX + 1,posY + 1] % 10 == tileColor) {
                totalColor++;
            }

            if(gridColors[posX - 1,posY + 1] % 10 == tileColor) {
                totalColor++;
            }
            
            if(gridColors[posX - 1,posY] % 10 == tileColor) {
                totalColor++;
            }
        }

        return totalColor;
    }
}
