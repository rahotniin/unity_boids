using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using static UnityEditor.PlayerSettings;

public class BoidHandler : MonoBehaviour {
	public Material material;
	RenderParams renderParams;
	Mesh mesh;
	
	[Header("Debugging")]
	public float debugScale = 1.0f;
	[Header("Spawn Config:")]
	public int numBoids = 100;
	public float spawnRadius = 10f;
	[Header("Default Boid Stats")]
	public float defaultSpeed = 0.5f;
	public float angularSpeed = Mathf.Deg2Rad * 90f;
	public Vector3 defaulScale = new Vector3(0.1f, 0.1f, 1.0f);
	public float maxAcceleration = 0.1f;
    //[SerializeField] public float flockRange = 5f;
    //[SerializeField] public float personalSpace = 1f;
    [Header("Flocking Params")]
	public float flockRange = 5f;
    public float personalSpace = 2f;
    public float alignment_weight = 1.0f;
	public float separation_weight = 1.0f;
	public float cohesion_weight = 1.0f;
	public float homing_weight = 1.0f;
	public float momentum_weight = 1.0f;

	List<Boid> boids;
	List<Matrix4x4> matrices;

	void Start() {
		Initialise();
		GenerateBoids();
	}

	void Initialise() {
		renderParams = new RenderParams(material);
		mesh = Boid.Mesh;

		boids = new List<Boid>();
		matrices = new List<Matrix4x4>();
	}

	void GenerateBoids() {
		for (int i = 0; i < numBoids; i++) {
			Vector3 rnd_pos = Random.insideUnitCircle * spawnRadius;
			float size = 1f;//Random.Range(0.5f, 1.5f);
            float speed = 1f;//Random.Range(0.5f, 2.0f) / size;
			Vector3 scale = new Vector3(1f / speed * defaulScale.x, speed * defaulScale.y, 1f) * size;
			Vector3 rnd_vel = Random.insideUnitCircle.normalized;
			
			
			CreateBoid(rnd_pos, rnd_vel, speed, scale);
		}
	}
	void CreateBoid(Vector3 position, Vector3 velocity, float cruisingSpeed, Vector3 scale) {
		Boid boid = new Boid(position, velocity, cruisingSpeed, scale);

		boids.Add(boid);
		matrices.Add(boid.ToMatrix());
	}

	void Update() {
		UpdateBoids();
		RenderBoids();
	}

	void UpdateBoids() {
		for (int i = 0; i < numBoids; i++) {
			Boid boid = boids[i];

			Vector3 closestPos = Vector3.zero;
			float minSqrDst = float.MaxValue;

			Vector3 sum_pos = Vector3.zero;
			Vector3 sum_vel = Vector3.zero;
			
			int flockSize = 0;
			for (int j = 0; j < numBoids && j != i; j++) {
				Boid flockmate = boids[j];
				
				float sqrDst = (flockmate.pos - boid.pos).sqrMagnitude;
				if (sqrDst < flockRange) {
					flockSize++;

					sum_pos += flockmate.pos;
					sum_vel += flockmate.vel;

					if (sqrDst < minSqrDst) {
						closestPos = flockmate.pos;
						minSqrDst = sqrDst;
					}
				}
			}
			
			Vector3 targetVel = Vector3.zero;

			Vector3 momentum = matrices[i].rotation * Vector3.up * boid.cruisingSpeed;
			targetVel += momentum;

            //Vector3 homing = -boid.pos * homing_weight;
            //targetVel += homing;

            if (flockSize > 0) {
                Vector3 avg_pos = sum_pos / flockSize;
                Vector3 cohesion = avg_pos - boid.pos;
                targetVel += cohesion * cohesion_weight;

                Vector3 alignment = sum_vel;
                targetVel += alignment * alignment_weight;

                Vector3 separation = Vector3.zero;
                if (minSqrDst < personalSpace * personalSpace) {
                    separation = (boid.pos - closestPos) / minSqrDst;
                }
                targetVel += separation * separation_weight;
            }

            targetVel = targetVel.normalized * boid.cruisingSpeed;
            Vector3 delta = (targetVel - boid.vel);
			if (delta.sqrMagnitude > maxAcceleration * Time.deltaTime) {
				Vector3 acceleration = delta.normalized * maxAcceleration;
				boid.vel += acceleration * Time.deltaTime;
                Debug.DrawLine(boid.pos, boid.pos + acceleration * debugScale, Color.green);
            } else {
				boid.vel = targetVel;
			}

            Debug.DrawLine(boid.pos, boid.pos + boid.vel * debugScale, Color.red);
            Debug.DrawLine(boid.pos, boid.pos + targetVel * debugScale, Color.blue);

            boids[i].pos += boids[i].vel * Time.deltaTime;
			matrices[i] = boids[i].ToMatrix();
		}
	}

	void RenderBoids() {
		Graphics.RenderMeshInstanced(renderParams, mesh, 0, matrices);
	}

	class Boid {
		public Vector3 pos;
		public float cruisingSpeed;
		public Vector3 vel;

        public Vector3 scale;

		public Boid(Vector3 position, Vector3 velocity, float cruisingSpeed, Vector3 scale) {
			this.pos = position;
			this.cruisingSpeed = cruisingSpeed;

			vel = velocity;
			
			this.scale = scale;
		}

		public Matrix4x4 ToMatrix() {
			return Matrix4x4.TRS(pos, Quaternion.LookRotation(Vector3.forward, vel), scale);
		}

		public static Mesh Mesh {  get {
				Mesh mesh = new Mesh();
				mesh.vertices = new Vector3[] {
					new Vector3(0f, 1f, 0f),
					new Vector3(1f, -1f, 0f),
					new Vector3(0f, -0.5f, 0f),
					new Vector3(-1f, -1f, 0f),
				};
				mesh.triangles = new int[] {
					0, 1, 2, 0, 2, 3,
				};
				return mesh;
			}
		}
	}
}
