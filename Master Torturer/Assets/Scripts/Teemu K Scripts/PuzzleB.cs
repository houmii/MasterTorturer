using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;
using Cinemachine;
public enum PuzzleState { WaitingPieces, InProgress, Unfinished, Finished }
public class PuzzleB : MonoBehaviour {
    public PuzzleState puzzleState;
    public Transform[] foundPieceSlots, foundPieceSlotsInUse, slabSlots;
    public int foundPieceCount, piecesNeeded;
    [Tooltip("Maximum allowed offset distance for piece to still snap into correct slot")][SerializeField] float pieceDistanceValue;
    [SerializeField] int piecesInCorrectSpot;
    bool originSpotChecked;
    Vector3 originalSpot;

    [SerializeField] GameObject[] puzzleRewards;
    [SerializeField] Transform[] rewardSpawnSpots;
    [SerializeField] CinemachineClearShot tableCam;

    Ray mouseRay;

    GameManager gm;

    void Start() {
        puzzleState = PuzzleState.WaitingPieces;
        gm = FindObjectOfType<GameManager>();

        foundPieceSlots = GameObject.Find("Found piece area").GetComponentsInChildren<Transform>();
        foundPieceSlotsInUse = new Transform[foundPieceSlots.Length];
        slabSlots = GameObject.Find("Stone slab").GetComponentsInChildren<Transform>();
    }

    void Update() {
        //Mouse ray origin
        mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (foundPieceCount == piecesNeeded & puzzleState == PuzzleState.WaitingPieces) {
            GameObject triggerArea = GameObject.Find("Puzzle trigger area");
            triggerArea.GetComponent<PuzzleBTrigger>().enabled = false;
            triggerArea.GetComponent<Collider>().enabled = false;
            triggerArea.GetComponent<MeshRenderer>().enabled = false;

            //Activate slab slots "holograms"
            for (int m = 1; m < 7; m++) {
                slabSlots[m].GetComponent<MeshRenderer>().enabled = true;
            }

            puzzleState = PuzzleState.InProgress;
        }

        //Switch that controls player and puzzle camera
        switch (puzzleState) {
            case PuzzleState.InProgress:
            tableCam.Priority = 11;
            Cursor.lockState = CursorLockMode.Confined;
            break;
            case PuzzleState.Unfinished or PuzzleState.Finished:
            tableCam.Priority = 1;
            Cursor.lockState = CursorLockMode.Locked;
            break;
        }

        RaycastHit ray;
        //Set ray to hit only Puzzle piece layer
        int layerMask = 1 << 8;
        bool rayHit = Physics.Raycast(mouseRay.origin, mouseRay.direction, out ray, 5f, layerMask);
        
        //Puzzle piece moving in the table
        if (Input.GetMouseButton(0) && puzzleState == PuzzleState.InProgress) {
            if (rayHit) {
                //Look at where piece was and store it's point for possible reset
                if (!originSpotChecked) {
                    originalSpot = ray.transform.position;
                    originSpotChecked = true;
                }

                Debug.DrawRay(mouseRay.origin, mouseRay.direction * ray.distance, Color.red);
                ray.transform.position = new Vector3(ray.point.x, ray.transform.position.y, ray.point.z);

                //Rotate while holding a piece, kind of works. Commented out, because it's buggy
                //if(Input.GetKeyDown(KeyCode.E)) ray.transform.Rotate(0f, 90f, 0f, Space.World);
            }
            else {
                Debug.DrawRay(mouseRay.origin, mouseRay.direction * ray.distance, Color.white);
            }
        }
        //Mouse button release
        else if (Input.GetMouseButtonUp(0) && puzzleState == PuzzleState.InProgress){
            try {
                int pieceId = ray.transform.GetComponent<PuzzleBPiece>().pieceId;
                
                //Check if piece is close enough to it's spot
                if (ray.transform.position.x - slabSlots[pieceId].position.x >= -pieceDistanceValue && ray.transform.position.x - slabSlots[pieceId].position.x <= pieceDistanceValue
                    && ray.transform.position.z - slabSlots[pieceId].position.z >= -pieceDistanceValue && ray.transform.position.z - slabSlots[pieceId].position.z <= pieceDistanceValue) {
                    Debug.Log("Close enough");
                    ray.transform.position = slabSlots[pieceId].position;
                    slabSlots[pieceId].GetComponent<MeshRenderer>().enabled = false;

                    //Set piece tag, layer back to default and destroy piece rigidbody so it cannot be picked up again or moved.
                    ray.transform.tag = "Untagged";
                    ray.transform.gameObject.layer = 0;
                    Destroy(ray.rigidbody);

                    piecesInCorrectSpot++;
                    if (piecesInCorrectSpot == piecesNeeded) {
                        AudioManager aM = FindObjectOfType<AudioManager>();
                        TortureDeviceScriptCopyTK2 puzzleA = FindObjectOfType<TortureDeviceScriptCopyTK2>();
                        puzzleState = PuzzleState.Finished;
                        //Instaniate rewards and "mark" puzzle as finished in GameManager
                        Instantiate(puzzleRewards[0], rewardSpawnSpots[0].position, rewardSpawnSpots[0].transform.rotation);
                        
                        //Item that is added to Puzzle A Torture Devices list.
                        puzzleA.AddTortuteItemToList(Instantiate(puzzleRewards[1], rewardSpawnSpots[1].position, rewardSpawnSpots[1].transform.rotation), 0);
                        puzzleA.puzzleBFinished = true;
                        gm.PuzzleDone();

                        //Lastly play puzzle done sound
                        aM.Play("Puzzledone");
                    }
                    
                }
                else {
                    Debug.Log("Not close enough");
                    Debug.Log("Distance from correct slot (Vector x): " + (ray.transform.position.x - slabSlots[pieceId].position.x));
                    Debug.Log("Distance from correct slot (Vector z): " + (ray.transform.position.z - slabSlots[pieceId].position.z));
                    //Reset the piece back to its original spot
                    if (originSpotChecked) {
                        ray.transform.position = originalSpot;
                        originSpotChecked = false;
                    }
                }
            }
            catch {
                Debug.Log("Not releasing mouse button from puzzle piece OR some things are missing from the scene. GameManager for example");
            }
        }

    }
}
