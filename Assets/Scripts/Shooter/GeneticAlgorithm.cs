using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public enum CrossoverType
{
    Intercambio,
    Promedio,
    Aleatorio
}

[Serializable]
public class GeneticAlgorithm
{
    public List<Individual> population;
    private int currentIndex;
    public int CurrentGeneration;
    public int MaxGenerations;
    public string Summary;

    private CrossoverType crossoverType;

    public GeneticAlgorithm(int numberOfGenerations, int populationSize, CrossoverType tipoCruce)
    {
        CurrentGeneration = 0;
        MaxGenerations = numberOfGenerations;
        crossoverType = tipoCruce;
        Summary = "";
        GenerateRandomPopulation(populationSize);
    }

    public void GenerateRandomPopulation(int size)
    {
        population = new List<Individual>();
        for (int i = 0; i < size; i++)
        {
            population.Add(new Individual(Random.Range(0f, 90f), Random.Range(5f, 25f)));
        }
        StartGeneration();
    }

    public Individual GetFittest()
    {
        population.Sort();
        return population[0];
    }

    public void StartGeneration()
    {
        currentIndex = 0;
        CurrentGeneration++;
    }

    public Individual GetNext()
    {
        if (currentIndex >= population.Count)
        {
            EndGeneration();
            if (CurrentGeneration >= MaxGenerations)
            {
                Debug.Log(Summary);
                return null;
            }
            StartGeneration();
            return population[0];
        }
        return population[currentIndex++];
    }

    public void EndGeneration()
    {
        population.Sort();
        Summary += $"{GetFittest().fitness};";

        if (CurrentGeneration < MaxGenerations)
        {
            Crossover();
            Mutation();
        }
    }

    public void Crossover()
    {
        Individual parent1 = population[0];
        Individual parent2 = population[1];

        population.RemoveRange(population.Count - 2, 2);

        Individual child1 = null;
        Individual child2 = null;

        switch (crossoverType)
        {
            case CrossoverType.Intercambio:
                child1 = new Individual(parent1.degree, parent2.strength);
                child2 = new Individual(parent2.degree, parent1.strength);
                break;
            case CrossoverType.Promedio:
                child1 = new Individual((parent1.degree + parent2.degree) / 2f, parent1.strength);
                child2 = new Individual(parent1.degree, (parent1.strength + parent2.strength) / 2f);
                break;
            case CrossoverType.Aleatorio:
                child1 = new Individual(Random.value > 0.5f ? parent1.degree : parent2.degree, Random.value > 0.5f ? parent1.strength : parent2.strength);
                child2 = new Individual(Random.value > 0.5f ? parent1.degree : parent2.degree, Random.value > 0.5f ? parent1.strength : parent2.strength);
                break;
        }
        population.Add(child1);
        population.Add(child2);
    }

    public void Mutation()
    {
        foreach (var individual in population)
        {
            if (Random.Range(0f, 1f) < 0.05f)
                individual.degree = Mathf.Clamp(individual.degree + Random.Range(-10f, 10f), 0f, 90f);

            if (Random.Range(0f, 1f) < 0.05f)
                individual.strength = Mathf.Clamp(individual.strength + Random.Range(-5f, 5f), 0f, 30f);
        }
    }
}