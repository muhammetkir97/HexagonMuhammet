using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class hexTile : MonoBehaviour
{
    
    //public variables for assign values on editor script
    public Color[] colors;
    public int colorCount;

    public float colOffset,rowOffset;

    public bool isBomb;
    public int timer;
    Transform bombNumbers;


  
    public int colorNumber = -1;

    //change the value if the tile object changed.
    float scanSize = 1.4f;


    Animator tileAnimator;

 

    void Start() {
        tileAnimator = transform.GetComponent<Animator>();

        //get default position according its name
        string[] coords = transform.name.Split('-');
        int xCoord = int.Parse(coords[0]);
        int yCoord = int.Parse(coords[1]);
        Vector3 defaultPos = new Vector3(xCoord*colOffset,yCoord*rowOffset,1);

        //if its x position is a odd number, add half offset (because of grid structure)
        if(xCoord%2 == 1) {
            defaultPos.y += rowOffset/2f;
        }

        //it is is on different place that it has to be, drop it 
        if(defaultPos != transform.position) {
            dropControll();
        }
    
    }

    public int setBombTimer(int time) {
        bombNumbers = transform.GetChild(3); 
        //if send -1 as time, decrease it
        if(time == -1) {
            time = timer - 1;
        }
        //get all number sprites, and make visible selected number
        for(int num = 0; num < 10;num++) {
            //if it reach the sended value, assign it as timer, and make this number sprite visible
            if(num == time) {
                timer = time;
                bombNumbers.GetChild(num).gameObject.GetComponent<SpriteRenderer>().color = new Color(1,1,1,1);
            }
            else {
                //make invisible other numbers
                bombNumbers.GetChild(num).gameObject.GetComponent<SpriteRenderer>().color = new Color(1,1,1,0);

            }
        }
        
        //set new color number according to its color and timer.
        //this helps when saving to array and controlling
        colorNumber = (colorNumber % 10) + (10 * timer);
        return timer;
    }





    void Update() {
        
    }

    //assign starting values
    public void assingValues(Color[] colorArray,int colorCnt,float col,float row,bool bomb) {
        colors = colorArray;
        colorCount = colorCnt;

        colOffset = col;
        rowOffset = row;
       
        isBomb = bomb;
    }


    //assign color for new tiles that created from gameSystem
    public void setRandomColor(int value) {
        //assign value to selectedColor
        int selectedColor = value;

        //if it is -1, assign randomly
        if(value == -1) {
            selectedColor = Random.Range(0,colorCount) + (10 * timer);
        }
 
        transform.GetChild(0).GetComponent<SpriteRenderer>().color = colors[selectedColor%10];
        colorNumber = selectedColor;
  
    }



    //assign starting color
    public int setColor() {
        int[] colorNumbers = new int[colorCount];
        colorNumbers = getColorsAround();

        //select a random color first, if there are more than 1 color on around, randomize again
        int selectedColor = Random.Range(0,colorCount);
        while(colorNumbers[selectedColor] > 1) {
            selectedColor = Random.Range(0,colorCount);
        }
        
        //get color property of this object and change it.
        transform.GetChild(0).GetComponent<SpriteRenderer>().color = colors[selectedColor];
        colorNumber = selectedColor;
        return colorNumber;
    }

    public int getColor() {
        return colorNumber;
    }

    //check that is tile adjacent with obj tile.
    public bool isAdjacentWith(GameObject obj) {
        Collider2D[] surroundingTiles = Physics2D.OverlapCircleAll(transform.position,scanSize);
        foreach (Collider2D tile in surroundingTiles) { 
            if(tile.transform.parent.gameObject == obj) {
                return true;
            }
        }
        return false;
    }

    public List<GameObject> checkColorsAround() {
        //this is for returning objects
        List<GameObject> adjacentTiles = new List<GameObject>();

        List<GameObject> controllTiles = new List<GameObject>();

            int[] colorNumbers = new int[colorCount];
            colorNumbers = getColorsAround();

            //if there are more than 2 tiles in same color, control that they are adjacent with each other
            if(colorNumbers[colorNumber % 10] > 2) {
                Collider2D[] surroundingTiles = Physics2D.OverlapCircleAll(transform.position,scanSize);
                foreach (Collider2D tile in surroundingTiles) {
                    hexTile tileCode = tile.transform.parent.GetComponent<hexTile>();
                    //if their colors are match, and they are not the same tiles, assign it to controll tiles
                    if((tileCode.colorNumber % 10 == colorNumber % 10) && (tile.transform.parent != transform)) {
                        controllTiles.Add(tile.transform.parent.gameObject); 
                    }
                }

                //add adjacent tiles to adjacentTiles list
                foreach(GameObject tile1 in controllTiles) {
                    //use status for add every tile for only one time
                    bool status = false;
                    foreach(GameObject tile2 in controllTiles) {
                        if(tile1 != tile2) {
                            if(tile1.GetComponent<hexTile>().isAdjacentWith(tile2) && !status) {
                                status = true;
                                adjacentTiles.Add(tile1);
                            }
                        }
                    }
                }

                //add tile itself
                adjacentTiles.Add(gameObject);
            }
        


        //if there are less than 3 tiles, return empty list
        if(adjacentTiles.Count < 3) {
            return new List<GameObject>();
        }
    
        return adjacentTiles;

    }

    //get colors of surrounding tiles. 
    int[] getColorsAround() {
       
        int[] colorNumbers = new int[colorCount];

        Collider2D[] surroundingTiles = Physics2D.OverlapCircleAll(transform.position,scanSize);
        //Debug.Log("--------------- " + transform.name);
        foreach (Collider2D tile in surroundingTiles) {
            //Debug.Log(tile.transform.parent.name);
            int tmpNumber = tile.transform.parent.GetComponent<hexTile>().getColor() % 10;

            //if it is not assigned yet, the color value is -1;
            if(tmpNumber != -1) {
                colorNumbers[tmpNumber]++;
            }
        }
        
        return colorNumbers;
    }


    //get the angle as right is 0 deg. and left is 180 deg.
    float getTileAngle(Vector2 pos1,Vector2 pos2) {
        float angle = Vector2.SignedAngle(transform.right,pos1 - pos2);

        //SignedAngle gives value between 180,-180. Convert it to 0-360
        if(angle < 0) {
            angle = 180 + (180 + angle);
        }

        return angle;
    }
  

    public GameObject[] getTiles(Vector2 hitPosition) {
        //get all tiles expect bombs (in layer 8)
        int layerMask = 1 << 8;
        layerMask = ~layerMask;

        Collider2D[] surroundingTiles = Physics2D.OverlapCircleAll(transform.position,scanSize,layerMask);

        //if there are more than 2 tiles in, around make the operations
        if(surroundingTiles.Length > 2) {
            GameObject[] returnTiles = new GameObject[3];
            GameObject[] tiles = new GameObject[6];

            //assign all detected tiles with their angle, (0 deg. to 0, 60 deg. to 1 ...)
            foreach (Collider2D tile in surroundingTiles) {
                string tileName = tile.transform.parent.name;

                //it can detect itself, make this operation to other tiles.
                if(tileName != transform.name) {
                    float tileAngle = getTileAngle(tile.transform.position,transform.position);
                    int tileNum = Mathf.FloorToInt(tileAngle / 60);
                    tiles[tileNum] = tile.transform.parent.gameObject;
                }
            }

            
            returnTiles[0] = gameObject;
            bool isCompleted = false;

            //get angle from the center of this tile to touched position.
            float hitAngle = getTileAngle(hitPosition,transform.position);

            //if there is not a tile in touched direction, change the touch angle,
            //if randomDirection = 0, change on clockwise (+60), else continue on counter clockwise(-60)
            int randomDirection = Random.Range(0,2);
            while(!isCompleted) {
                
                //if there is not any error, loop will end
                isCompleted = true;

                //convert angle to tile number
                int tileNumber1 = (int)(hitAngle - (hitAngle%60)) / 60;
                int tileNumber2 = (tileNumber1 < 5) ? (tileNumber1+1) : 0;

                //if all tiles is assigned, assign them to return value.
                if(tiles[tileNumber1] != null && tiles[tileNumber2] != null) {
                    returnTiles[1] = tiles[tileNumber1];
                    returnTiles[2] = tiles[tileNumber2];
                }
                else {
                    //if there are missing tiles, change the angle and set isCompleted as false for continue loop.
                    if(randomDirection == 0) {
                        isCompleted = false;
                        hitAngle += 60;
                        if(hitAngle >= 360) {
                            hitAngle = hitAngle - 360;
                        }
                    }
                    else {
                        isCompleted = false;
                        hitAngle -= 60;
                        if(hitAngle < 0) {
                            hitAngle = 360 + hitAngle;
                        }
                    }

                }
            }
            return returnTiles;
        }
        else {
            return null;
        }

    
    }



    //it drop tiles if any empty places on bottom of tile
    public void dropControll() {
        string[] coords = transform.name.Split('-');
        int xCoord = int.Parse(coords[0]);
        int yCoord = int.Parse(coords[1]);

        //start checking from 0 to position of y to this tile
        for(int y = 0; y < yCoord; y++) {
            string bottomTileName = xCoord + "-" + y;

            //if there is empy places, drop this tile to that position
            if(GameObject.Find(bottomTileName) == null) {
                //assign its new name
                transform.name = bottomTileName;
                
                //set position as same as position of empy place
                Vector3 newPos = transform.position;
                newPos.y = rowOffset * y;

                //if the tile on the high column (1,3,5 ..), needed to add this offset
                if(xCoord % 2 == 1) {
                    newPos.y += rowOffset/2f;
                }

                resizeTile(-1);     //this for scale down effect
                dropTile(newPos);   //drop it
                break;
            }  
        } 
    }


    //drop tile to newPos
    void dropTile(Vector3 newPos) {
        //translate it downward
        transform.position += Vector3.down * 1.5f;

        //if the position is reached desired position (sometimes it can pass through)
        if(transform.position.y < newPos.y) {
            transform.position = newPos;    //assign desired position
            resizeTile(1);                  //scale up effect
        }
        else {
            StartCoroutine(waitForDropping(newPos));    //waiting for smooth transition
        }

    }

    //wait for a while and call dropTile again with same value
    IEnumerator waitForDropping(Vector3 pos) {
        yield return new WaitForFixedUpdate();
        dropTile(pos);
    }


    //enable or disable halo. it using for selection and unselection operations
    public void tileOutline(bool isSelected) {
        tileAnimator.SetBool("isSelected",isSelected);
    }

    //resize effect
    public void resizeTile(int size) {
        if(size == 1) {
            tileAnimator.SetTrigger("scaleUp");
        }
        else {
            tileAnimator.SetTrigger("scaleDown");
        }
        
    }
}
