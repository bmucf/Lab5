namespace BrianMosqueraAvoiderPluginForUnity
{
    using JetBrains.Annotations;
    using System.Collections;
    using System.Collections.Generic;
    using Unity.VisualScripting;
    using UnityEngine;
    using UnityEngine.AI;

    public class Avoider : MonoBehaviour
    {
        [Header("Avoider Settings")]
        public int speed;
        public int range;
        public GameObject stalker;
        private NavMeshAgent runner;

        [Header("Poisson Disc Sampler Settings")]
        public float sampleRadius = 25f;
        public float spaceBetweenPoints = 2f;
        public bool turnGizmosOff;
        private List<Vector3> poissonPositions3D = new List<Vector3>();
        public List<Vector3> validPoints = new List<Vector3>();

        [Header("Obstacle Settings")]
        public float cubeSize = 2f;
        public int numberOfCubes = 10;
        public Vector2 spawnRange = new Vector2(-23f, 23f);

        private Vector3 goal;
        public GameObject cubeObstacle;

        void OnEnable()
        {
            runner = GetComponent<NavMeshAgent>();

            if (GetComponent<NavMeshAgent>() == null)
            {
                Debug.LogWarning($"Put a Nav Mesh Agent component on {gameObject.name} and bake a Nav Mesh");
            }
        }

        private void Start()
        {
            SpawnCubes();
        }

        private void Update()
        {
            // Make the objects face each other
            stalker.transform.LookAt(transform.position);
            transform.LookAt(stalker.transform.position);

            // Only generate Poisson points once when needed
            if (poissonPositions3D.Count == 0)
            {
                GeneratePoissonPoints();
            }

            // Clear valid points each frame
            validPoints.Clear();

            foreach (Vector3 point in poissonPositions3D)
            {
                Vector3 direction = (point - stalker.transform.position).normalized;
                float distanceToPoint = Vector3.Distance(stalker.transform.position, point);

                if (Physics.Raycast(stalker.transform.position, direction, out RaycastHit hit, distanceToPoint))
                {
                    // Only consider points where the ray hits a cube first
                    if (hit.collider.gameObject.CompareTag("Obstacle"))
                    {
                        if (!validPoints.Contains(point))
                        {
                            validPoints.Add(point);
                            Debug.DrawLine(stalker.transform.position, point, Color.green);
                        }
                    }
                    else
                    {
                        Debug.DrawLine(stalker.transform.position, point, Color.red);
                    }
                }
                else
                {
                    // No obstruction → point is visible → ignore
                    Debug.DrawLine(stalker.transform.position, point, Color.red);
                }
            }

            // Move runner to the closest valid point
            if (validPoints.Count > 0)
            {
                Vector3 closestPoint = validPoints[0];
                float closestDist = Vector3.Distance(transform.position, closestPoint);

                foreach (Vector3 potential in validPoints)
                {
                    float dist = Vector3.Distance(transform.position, potential);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closestPoint = potential;
                    }
                }

                goal = closestPoint;
                runner.SetDestination(goal);
            }
        }



        private void GeneratePoissonPoints()
        {
            var sampler = new PoissonDiscSampler(sampleRadius * 2, sampleRadius * 2, spaceBetweenPoints);
            foreach (Vector2 point in sampler.Samples())
            {
                float offsetX = point.x - sampleRadius; // shift to be centered
                float offsetZ = point.y - sampleRadius;

                Vector3 point3D = new Vector3(offsetX, 0, offsetZ);
                poissonPositions3D.Add(point3D);
            }
        }

        //public void GeneratePoissonPoints()
        //{
        //    var sampler = new PoissonDiscSampler(sampleRadius * 2, sampleRadius * 2, spaceBetweenPoints);
        //    foreach (Vector2 point in sampler.Samples())
        //    {
        //        float offsetX = point.x - sampleRadius; // shift to be centered
        //        float offsetZ = point.y - sampleRadius;

        //        Vector3 point3D = new Vector3(offsetX, 0, offsetZ);
        //        poissonPositions3D.Add(point3D);
        //    }
        //}

        private void SpawnCubes()
        {
            for (int i = 0; i < numberOfCubes; i++)
            {
                float x = Random.Range(spawnRange.x, spawnRange.y);
                float z = Random.Range(spawnRange.x, spawnRange.y);
                Vector3 pos = new Vector3(x, 1f, z);

                GameObject cube = GameObject.Instantiate(cubeObstacle);
                cube.transform.position = pos;
                cube.tag = "Obstacle";
            }
        }

        private void OnDrawGizmos()
        {
            if (turnGizmosOff) return;

            foreach (Vector3 pos in poissonPositions3D)
            {
                float distance = Vector3.Distance(transform.position, pos);

                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(pos, 0.25f);

                if (distance <= 7)
                {
                    Gizmos.DrawLine(transform.position, pos);
                }
            }

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, range);
        }
    }

}

