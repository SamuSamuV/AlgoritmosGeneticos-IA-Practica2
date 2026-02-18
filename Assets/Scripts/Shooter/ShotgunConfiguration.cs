using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ShotgunConfiguration : MonoBehaviour
{
    public float XDegrees;

    public float Strength;

    public Rigidbody ShotSpherePrefab;
    public Transform ShotPosition;

    public Transform Target;

    public GeneticAlgorithm Genetic;
    public Individual CurrentIndividual;


    private bool _ready;
    // Start is called before the first frame update
    void Start()
    {
        Time.timeScale = 50f;

        Genetic = new GeneticAlgorithm(10,10);
        
        _ready = true;
    }

    public void ShooterConfigure(float xDegrees, float strength)
    {
        XDegrees = xDegrees;
        Strength = strength;
    }

    public void GetResult(float data)
    {
        Debug.Log($"Result {data}");
        CurrentIndividual.fitness = data;
        _ready = true;
    }

    public void Shot()
    {
        _ready = false;

        transform.eulerAngles = new Vector3(XDegrees, 0,0);
        var shot = Instantiate(ShotSpherePrefab, ShotPosition);
        shot.gameObject.GetComponent<TargetTrigger>().Target = Target;
        shot.gameObject.GetComponent<TargetTrigger>().OnHitCollider += GetResult;
        shot.isKinematic = false;
        var force = transform.up * Strength;
        shot.AddForce(force,ForceMode.Impulse);
    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Time.timeScale = 1f;
            CurrentIndividual = Genetic.GetFittest();
            ShooterConfigure(CurrentIndividual.degree, CurrentIndividual.strength);
            Shot();
        }

        if (_ready)
        {
            CurrentIndividual = Genetic.GetNext();
            if (CurrentIndividual != null)
            {
                ShooterConfigure(CurrentIndividual.degree,CurrentIndividual.strength);
                Shot();
            }
            else
            {
                CurrentIndividual = Genetic.GetFittest();
                _ready = false;
            }
        }
    }
}
