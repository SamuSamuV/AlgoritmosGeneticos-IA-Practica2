using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShotgunConfiguration : MonoBehaviour
{
    public CrossoverType TipoDeCruce = CrossoverType.Intercambio;
    public int TamanoPoblacion = 10;
    public int NumeroGeneraciones = 50;

    public Rigidbody ShotSpherePrefab;
    public Transform ShotPosition;
    public Transform Target;
    public Transform ParedFondo;

    private GeneticAlgorithm Genetic;
    private Individual CurrentIndividual;

    private bool waitingForShotResult;
    private bool finished = false;
    private GameObject currentBullet;

    public float CurrentDegree;
    public float CurrentStrength;

    void Start()
    {
        Time.timeScale = 100f;

        Genetic = new GeneticAlgorithm(NumeroGeneraciones, TamanoPoblacion, TipoDeCruce);

        MoverTargetAleatorio();

        waitingForShotResult = false;
        finished = false;
    }

    public void MoverTargetAleatorio()
    {
        if (ParedFondo != null)
        {
            float limitesX = (ParedFondo.localScale.x / 2) - 1f;
            float limitesY = ParedFondo.localScale.y;
            float randomX = Random.Range(-limitesX, limitesX);
            float randomY = Random.Range(1f, limitesY - 1f);

            Target.localPosition = new Vector3(randomX, randomY, Target.localPosition.z);
        }
    }

    public void ShooterConfigure(float xDegrees, float strength)
    {
        CurrentDegree = xDegrees;
        CurrentStrength = strength;
    }

    public void GetResult(float data)
    {
        if (finished) return;

        Debug.Log($"Result {data}");

        if (CurrentIndividual != null)
        {
            CurrentIndividual.fitness = data;
        }

        waitingForShotResult = false;
        if (currentBullet != null) Destroy(currentBullet);
    }

    public void Shot()
    {
        waitingForShotResult = true;

        transform.localRotation = Quaternion.Euler(-CurrentDegree, 0, 0);

        var shotObj = Instantiate(ShotSpherePrefab, ShotPosition.position, ShotPosition.rotation);
        currentBullet = shotObj.gameObject;

        var trigger = shotObj.GetComponent<TargetTrigger>();
        trigger.Target = Target;
        trigger.OnHitCollider += GetResult;

        shotObj.isKinematic = false;
        shotObj.AddForce(transform.forward * CurrentStrength, ForceMode.Impulse);

        StartCoroutine(FailsafeDestroy(shotObj.gameObject));
    }

    IEnumerator FailsafeDestroy(GameObject bullet)
    {
        yield return new WaitForSeconds(5f);
        if (bullet != null && waitingForShotResult && !finished)
        {
            GetResult(1000f);
        }
    }

    void Update()
    {
        if (finished) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Time.timeScale = 1f;
            var best = Genetic.GetFittest();
            if (best != null)
            {
                Debug.Log($"Best Angulo: {best.degree}, Best Fuerza: {best.strength}");
                ShooterConfigure(best.degree, best.strength);
                Shot();
            }
            return;
        }

        if (!waitingForShotResult)
        {
            CurrentIndividual = Genetic.GetNext();

            if (CurrentIndividual != null)
            {
                ShooterConfigure(CurrentIndividual.degree, CurrentIndividual.strength);
                Shot();
            }
            else
            {
                if (Genetic.CurrentGeneration >= Genetic.MaxGenerations)
                {
                    Debug.Log("Se ha terminado");
                    finished = true;
                    Time.timeScale = 1f;
                }
            }
        }
    }
}