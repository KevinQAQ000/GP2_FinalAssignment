using UnityEngine;

public enum PredatorType { Lion, Tiger }

public class PredatorIdentity : MonoBehaviour
{
    public PredatorType type;//This field is set in the Inspector to determine the predator's type
    private PredatorAI ai;//Reference to the PredatorAI component for modifying its attributes

    void Awake()
    {
        //Get the PredatorAI component
        ai = GetComponent<PredatorAI>();
        ApplyIdentity();
    }

    void ApplyIdentity()//Set AI parameters based on the predator type
    {
        if (ai == null) return;//If no AI component is found, return immediately

        switch (type)
        {
            case PredatorType.Lion:
                //Lion: King of the prairie, good endurance, long detection range, fast chase
                ai.detectRange = 50f;
                ai.sneakRange = 10f;
                ai.chaseRange = 8f;
                ai.runSpeed = 7f;           //Extremely fast sprint
                ai.hungerThreshold = 40f;
                break;

            case PredatorType.Tiger:
                //Tiger: Jungle ambusher, expert in stealth, enters sneak state earlier
                ai.detectRange = 36f;
                ai.sneakRange = 15f;        //Starts sneaking and lowering body from a far distance
                ai.chaseRange = 5f;         //Short burst distance
                ai.runSpeed = 5.5f;
                ai.hungerThreshold = 30f;
                break;
        }
    }
}