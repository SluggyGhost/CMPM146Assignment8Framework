using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class MapGenerator : MonoBehaviour
{
    public List<Room> rooms;
    public Hallway vertical_hallway;
    public Hallway horizontal_hallway;
    public Room start;
    public Room target;

    // Constraint: How big should the dungeon be at most
    // this will limit the run time (~10 is a good value 
    // during development, later you'll want to set it to 
    // something a bit higher, like 25-30)
    public int MAX_SIZE = 10;

    // set this to a high value when the generator works
    // for debugging it can be helpful to test with few rooms
    // and, say, a threshold of 100 iterations
    public int THRESHOLD = 100;

    // keep the instantiated rooms and hallways here 
    private List<GameObject> generated_objects;
    
    private int iterations;

    private const int MIN_ROOMS = 5;

    public void Generate()
    {
        // dispose of game objects from previous generation process
        foreach (var go in generated_objects)
        {
            Destroy(go);
        }
        generated_objects.Clear();

        generated_objects.Add(start.Place(new Vector2Int(0, 0)));
        List<Door> doors = start.GetDoors();
        List<Vector2Int> occupied = new List<Vector2Int> { new Vector2Int(0, 0) };
        iterations = 0;
        GenerateWithBacktracking(occupied, doors, 1);
    }


    bool GenerateWithBacktracking(List<Vector2Int> occupied, List<Door> doors, int depth)
    {
        iterations++;
        if (iterations > THRESHOLD) throw new System.Exception("Iteration limit exceeded");

        if (doors.Count == 0)
        {
            return occupied.Count >= MIN_ROOMS;
        }

        List<Door> remainingDoors = new List<Door>(doors);

        foreach (Door openDoor in remainingDoors)
        {
            Vector2Int doorGrid = openDoor.GetGridCoordinates();
            Vector2Int offset = DirectionToOffset(openDoor.GetDirection());
            Vector2Int newRoomPos = doorGrid + offset;

            if (occupied.Contains(newRoomPos)) continue;

            foreach (Room roomPrefab in rooms)
            {
                if (!roomPrefab.HasDoorOnSide(GetOppositeDirection(openDoor.GetDirection()))) continue;

                List<Door> candidateDoors = roomPrefab.GetDoors();
                foreach (Door candidateDoor in candidateDoors)
                {
                    Door matchingDoor = candidateDoor.GetMatching();
                    if (!openDoor.IsMatching(matchingDoor)) continue;

                    occupied.Add(newRoomPos);

                    List<Door> nextDoors = new List<Door>(doors);
                    nextDoors.Remove(openDoor);

                    foreach (Door d in candidateDoors)
                    {
                        if (!d.IsMatching(candidateDoor))
                        {
                            nextDoors.Add(d);
                        }
                    }

                    if (GenerateWithBacktracking(occupied, nextDoors, depth + 1))
                    {
                        GameObject newRoomGO = roomPrefab.Place(newRoomPos);
                        generated_objects.Add(newRoomGO);

                        Hallway hallway = openDoor.IsHorizontal() ? horizontal_hallway : vertical_hallway;
                        GameObject hallwayGO = hallway.Place(openDoor);
                        generated_objects.Add(hallwayGO);
                        return true;
                    }

                    occupied.Remove(newRoomPos);
                }
            }
        }

        return false;
    }

    Vector2Int DirectionToOffset(Door.Direction dir)
    {
        switch (dir)
        {
            case Door.Direction.NORTH: return new Vector2Int(0, 1);
            case Door.Direction.EAST: return new Vector2Int(1, 0);
            case Door.Direction.SOUTH: return new Vector2Int(0, -1);
            case Door.Direction.WEST: return new Vector2Int(-1, 0);
            default: return Vector2Int.zero;
        }
    }

    Door.Direction GetOppositeDirection(Door.Direction dir)
    {
        switch (dir)
        {
            case Door.Direction.NORTH: return Door.Direction.SOUTH;
            case Door.Direction.EAST: return Door.Direction.WEST;
            case Door.Direction.SOUTH: return Door.Direction.NORTH;
            case Door.Direction.WEST: return Door.Direction.EAST;
            default: return dir;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        generated_objects = new List<GameObject>();
        Generate();
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.gKey.wasPressedThisFrame)
            Generate();
    }
}
