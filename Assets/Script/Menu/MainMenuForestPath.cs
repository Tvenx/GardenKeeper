
using UnityEngine;

public class MainMenuForestPath : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MainMenuCharacterAnimation character;
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform endPoint;

    [Header("Forest Objects")]
    [SerializeField] private Transform[] bushes;
    [SerializeField] private Transform[] rocks;
    [SerializeField] private Transform[] treeTypeA;
    [SerializeField] private Transform[] treeTypeB;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float movementSpeed = 1f;

    private class MovingObject
    {
        public Transform transform;
        public float distance;
        public Vector3 offset;
    }

    private MovingObject[] objects;
    private Vector3 direction;
    private float pathLength;

    private void Start()
    {
        if (character == null || startPoint == null || endPoint == null)
        {
            Debug.LogError(
                "ForestPath: Assign Character, Start Point and End Point.",
                this
            );

            enabled = false;
            return;
        }

        direction = endPoint.position - startPoint.position;
        pathLength = direction.magnitude;

        if (pathLength < 0.01f)
        {
            Debug.LogError("ForestPath: Start and End must differ.", this);
            enabled = false;
            return;
        }

        direction.Normalize();

        int count = CountObjects();
        objects = new MovingObject[count];

        int index = 0;

        AddObjects(bushes, ref index);
        AddObjects(rocks, ref index);
        AddObjects(treeTypeA, ref index);
        AddObjects(treeTypeB, ref index);
    }

    private int CountObjects()
    {
        return Length(bushes) +
               Length(rocks) +
               Length(treeTypeA) +
               Length(treeTypeB);
    }

    private int Length(Transform[] array)
    {
        return array == null ? 0 : array.Length;
    }

    private void AddObjects(Transform[] source, ref int index)
    {
        if (source == null)
            return;

        foreach (Transform item in source)
        {
            if (item == null)
                continue;

            Vector3 relativePosition = item.position - startPoint.position;

            float distance = Vector3.Dot(relativePosition, direction);
            distance = Mathf.Repeat(distance, pathLength);

            Vector3 offset =
                relativePosition -
                Vector3.Dot(relativePosition, direction) * direction;

            objects[index++] = new MovingObject
            {
                transform = item,
                distance = distance,
                offset = offset
            };
        }
    }

    private void Update()
    {
        if (character == null || !character.IsWalking)
            return;

        foreach (MovingObject item in objects)
        {
            if (item == null || item.transform == null)
                continue;

            item.distance += movementSpeed * Time.deltaTime;

            if (item.distance >= pathLength)
                item.distance %= pathLength;

            item.transform.position =
                startPoint.position +
                direction * item.distance +
                item.offset;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (startPoint == null || endPoint == null)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(startPoint.position, 0.2f);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(endPoint.position, 0.2f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(startPoint.position, endPoint.position);
    }
}
