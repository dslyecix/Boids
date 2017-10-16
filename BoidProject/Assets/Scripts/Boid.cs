using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boid : MonoBehaviour
{
    public LayerMask boidMask;

    Flock parentFlock;
    public Flock ParentFlock { get { return parentFlock; } set { parentFlock = value; } }

    Vector3 velocity;
    public Vector3 Velocity { get { return velocity; } set { velocity = value; } }


    //Steering forces
    Vector3 cohesionSteeringForce;  
    Vector3 alignmentSteeringForce;
    Vector3 separationSteeringForce; 
    Vector3 boundarySteeringForce;
    Vector3 randomSteeringForce;
    Vector3 finalSteeringForce;    

    bool returningToBoundary;
    Vector3 returnToBoundsPoint;
    float randomTimer = 0;

    [SerializeField]
    List<Collider> neighbourList;

    //Run through the applicable behaviours and return the final movement vector
    public void UpdateBehaviour() { 

        //Get list of neighbours this frame
        UpdateNeighbours();

        //Debug for seeing connected neighbours
        // for (int i = 0; i < neighbours.Length; i++)
        // {
        //     Debug.DrawLine(transform.position,neighbours[i].transform.position);
        // }
       
        //Run each behaviour to get the steering contribution (they return Vector3.Zero if toggled off)
        cohesionSteeringForce = parentFlock.cohesionWeight * CalculateCohesion(neighbourList);
        alignmentSteeringForce = parentFlock.alignmentWeight * CalculateAlignment(neighbourList);
        separationSteeringForce = parentFlock.separationWeight * CalculateSeparation(neighbourList); 
        randomSteeringForce = RandomSteering();
        boundarySteeringForce = parentFlock.boundaryWeight * StayWithinBounds(parentFlock.flockController.transform.position);
        
        finalSteeringForce = Vector3.zero;
        finalSteeringForce = cohesionSteeringForce + 
                                alignmentSteeringForce + 
                                separationSteeringForce + 
                                randomSteeringForce + 
                                boundarySteeringForce;
        finalSteeringForce = Vector3.ClampMagnitude(finalSteeringForce,parentFlock.maxSteeringForce);
    }
    
    //Actually move (when told by the flock)
    public void ExecuteMovement () { 
        velocity += finalSteeringForce * Time.deltaTime;
        velocity = velocity.normalized * Mathf.Clamp(velocity.magnitude, parentFlock.minSpeed, parentFlock.maxSpeed);
        transform.Translate(velocity * Time.deltaTime, Space.World);
        transform.rotation = Quaternion.LookRotation(velocity, Vector3.up);
    }   

#region Behaviours

    //Move in the same direction as your neighbours, weighted based on their distance from you
    public Vector3 CalculateAlignment(List<Collider> neighbours) {
        if (parentFlock.useAlignment && neighbours.Count > 0) {
            Vector3 desiredVelocity = Vector3.zero;
            foreach (Collider col in neighbours) {
                float colDist = (col.transform.position - transform.position).magnitude;
                desiredVelocity += col.GetComponent<Boid>().velocity * ((1 + parentFlock.alignmentWeight) - (colDist / parentFlock.neighbourSearchRadius));       //Add up all individual neighbour velocities
            }
            desiredVelocity = desiredVelocity / neighbours.Count;
            
            return (desiredVelocity - velocity);
        }            
        return Vector3.zero;
    }

    //Move towards the centroid of your neighbours
    public Vector3 CalculateCohesion(List<Collider> neighbours) {
        if (parentFlock.useCohesion && neighbours.Count > 0) {
            Vector3 neighbourCentroid = Vector3.zero;
            foreach (Collider col in neighbours) {
                neighbourCentroid += col.transform.position;
            }
            neighbourCentroid = neighbourCentroid / neighbours.Count;
            Vector3 desiredVelocity = neighbourCentroid - transform.position;

            return (desiredVelocity - velocity);
        }
        return Vector3.zero;
    }

    //Avoid getting too close to your neighbours
    public Vector3 CalculateSeparation(List<Collider> neighbours) {
        Vector3 desiredVelocity = Vector3.zero;
        if (parentFlock.useSeparation && neighbours.Count > 0){
            foreach (Collider col in neighbours) {
                float colDist = (col.transform.position - transform.position).magnitude;
                if (colDist < parentFlock.separationRadius) {
                    desiredVelocity += (transform.position - col.transform.position) * (1 - (colDist / parentFlock.separationRadius));
                }
            }

            return (desiredVelocity - velocity);
        }
        return Vector3.zero;
    }

    public Vector3 StayWithinBounds(Vector3 center) {
        if (parentFlock.useBoundary) {
            Vector3 desiredVelocity = Vector3.zero;
            if ((transform.position - center).magnitude >= parentFlock.maximumBoundRadius && returningToBoundary == false) {     
                returnToBoundsPoint = transform.position + (center + Random.onUnitSphere * parentFlock.randomWeight - transform.position) * 0.5f;  
                returningToBoundary = true;
                desiredVelocity = returnToBoundsPoint - transform.position;                                                                        
            } else if (returningToBoundary == true && (transform.position - center).magnitude >= parentFlock.maximumBoundRadius * 0.6f) {                                                                               
                desiredVelocity = returnToBoundsPoint - transform.position;                                                              
            } else {                                                                                                                      
                 returningToBoundary = false;
                 desiredVelocity = velocity;                                                                                        
            }                                                                                                                            
            return (desiredVelocity - velocity);
        }
        return Vector3.zero;
    }   

    public Vector3 RandomSteering () {
        if (parentFlock.useRandom) {
            if (randomTimer >= 2f) {
                randomSteeringForce = randomSteeringForce.normalized + Random.onUnitSphere * parentFlock.randomWeight;
                randomSteeringForce = Vector3.ClampMagnitude(randomSteeringForce,velocity.magnitude);
                randomTimer = 0f;
                return randomSteeringForce;
            } else {
                randomTimer += Time.deltaTime;
                return randomSteeringForce;
            }
        } 
        return Vector3.zero;
    }
#endregion


    //TODO: Update this method so boids only detect neighbours AHEAD of them!!
    public void UpdateNeighbours () {
        neighbourList.Clear();
        Collider[] neighbourArray = Physics.OverlapSphere(transform.position, parentFlock.neighbourSearchRadius, boidMask);
        for (int i = 0; i < neighbourArray.Length; i++)
        {            
            if (neighbourArray[i].GetComponent<Boid>() != this){
                neighbourList.Add(neighbourArray[i]);
            }
        }
    }

    void OnDrawGizmos() {

        //Draw the various steering forces
        if (parentFlock.debugOn) {  
            Debug.DrawRay(transform.position,velocity.normalized * 0.5f);
     
            if (alignmentSteeringForce != Vector3.zero) Debug.DrawLine(transform.position,transform.position + alignmentSteeringForce / parentFlock.alignmentWeight + velocity, Color.red * 0.6f);
            if (cohesionSteeringForce != Vector3.zero) Debug.DrawLine(transform.position,transform.position + cohesionSteeringForce / parentFlock.cohesionWeight + velocity, Color.green * 0.6f);
            if (separationSteeringForce != Vector3.zero) Debug.DrawLine(transform.position,transform.position + separationSteeringForce / parentFlock.separationWeight + velocity, Color.blue * 0.5f);
            if (boundarySteeringForce != Vector3.zero) Debug.DrawLine(transform.position,transform.position + boundarySteeringForce / parentFlock.boundaryWeight + velocity, Color.magenta * 0.5f);
            if (randomSteeringForce != Vector3.zero) Debug.DrawLine(transform.position,transform.position + randomSteeringForce, Color.yellow * 0.9f);
        }

        //Draw the point to return towards when out of bounds
        // Gizmos.color = Color.blue * 0.1f;
        // Gizmos.DrawSphere(returnToBoundsPoint,0.1f);
        
        //Draw the radius boid will detect neighbours
        //Gizmos.color = Color.white * 0.1f;
        //Gizmos.DrawSphere(transform.position,parentFlock.neighbourRadius);
    }

   
}
