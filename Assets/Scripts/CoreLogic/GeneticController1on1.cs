using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Genome : IComparable<Genome>
{
    public float[] Weights;
    public float Fitness;

    public Genome(int numWeights)
    {
        Weights = new float[numWeights];
        for (int i = 0; i < numWeights; i++)
        {
            Weights[i] = UnityEngine.Random.Range(-1f, 1f);
        }
        Fitness = 0;
    }

    public int CompareTo(Genome other)
    {
        return other.Fitness.CompareTo(this.Fitness);
    }
}

public class GeneticController1on1 : AIController
{
    public bool isLearningMode = true;
    public int populationSize = 50;
    public int generations = 100;
    public float mutationRate = 0.1f;
    public int tournamentSize = 5;

    private List<Genome> population;
    private Genome bestGenome;
    private bool hasTrained = false;

    private const int NUM_WEIGHTS = 5;

    public new void Awake()
    {
        base.Awake();

        if (isLearningMode && !hasTrained)
        {
            StartTraining();
        }
    }

    public void Start()
    {
        Debug.Log("Start Genetic");
    }

    protected override void Think()
    {
        if (isLearningMode && !hasTrained)
        {
            StartTraining();
        }

        _attackToDo = ChooseBestAttack(_currentLogicState, bestGenome);
    }

    private void StartTraining()
    {
        InitializePopulation();

        for (int generation = 0; generation < generations; generation++)
        {
            EvaluatePopulation();

            population.Sort();
            bestGenome = population[0];

            if (bestGenome.Fitness > 1000f)
                break;

            CreateNextGeneration();
        }

        hasTrained = true;
        isLearningMode = false;
        Debug.Log($"Se ha terminado, mejor fitness: {bestGenome.Fitness}");
    }

    private void InitializePopulation()
    {
        population = new List<Genome>();
        for (int i = 0; i < populationSize; i++)
        {
            population.Add(new Genome(NUM_WEIGHTS));
        }
    }

    private void EvaluatePopulation()
    {
        foreach (var genome in population)
        {
            genome.Fitness = SimulateMatches(genome, 5);
        }
    }

    private float SimulateMatches(Genome genome, int numMatches)
    {
        float totalFitness = 0;

        for (int i = 0; i < numMatches; i++)
        {
            LogicState simState = new LogicState(GameState);
            int turnCounter = 0;

            while (!IsMatchOver(simState) && turnCounter < 50)
            {
                if (simState.PlayerIdxTurn == _player.Id)
                {
                    AttackInfo bestAttack = ChooseBestAttackInfo(simState, genome);
                    simState = SimulateAttack(simState, bestAttack, _player.Id);
                }
                else
                {
                    var opponentAttacks = GameState.ListOfPlayers.Players[_player.EnemyId].Attacks;
                    AttackInfo randomAttack = opponentAttacks[UnityEngine.Random.Range(0, opponentAttacks.Length)];
                    simState = SimulateAttack(simState, randomAttack, _player.EnemyId);
                }
                turnCounter++;
            }

            float myFinalHP = simState.HitPoints[_player.Id];
            float enemyFinalHP = simState.HitPoints[_player.EnemyId];

            float matchFitness = myFinalHP - enemyFinalHP;
            if (enemyFinalHP <= 0) matchFitness += 100;

            totalFitness += matchFitness;
        }

        return totalFitness / numMatches;
    }

    private void CreateNextGeneration()
    {
        List<Genome> nextGen = new List<Genome>();

        nextGen.Add(population[0]);
        nextGen.Add(population[1]);

        while (nextGen.Count < populationSize)
        {
            Genome parent1 = TournamentSelection();
            Genome parent2 = TournamentSelection();

            Genome child = Crossover(parent1, parent2);
            Mutate(child);

            nextGen.Add(child);
        }

        population = nextGen;
    }

    private Genome TournamentSelection()
    {
        Genome best = null;
        for (int i = 0; i < tournamentSize; i++)
        {
            Genome randomInd = population[UnityEngine.Random.Range(0, populationSize)];
            if (best == null || randomInd.Fitness > best.Fitness)
            {
                best = randomInd;
            }
        }
        return best;
    }

    private Genome Crossover(Genome p1, Genome p2)
    {
        Genome child = new Genome(NUM_WEIGHTS);
        for (int i = 0; i < NUM_WEIGHTS; i++)
        {
            child.Weights[i] = (UnityEngine.Random.value > 0.5f) ? p1.Weights[i] : p2.Weights[i];
        }
        return child;
    }

    private void Mutate(Genome genome)
    {
        for (int i = 0; i < NUM_WEIGHTS; i++)
        {
            if (UnityEngine.Random.value < mutationRate)
            {
                genome.Weights[i] += UnityEngine.Random.Range(-0.5f, 0.5f);
                genome.Weights[i] = Mathf.Clamp(genome.Weights[i], -1f, 1f);
            }
        }
    }

    private Attack ChooseBestAttack(LogicState state, Genome genome)
    {
        AttackInfo bestAttInfo = ChooseBestAttackInfo(state, genome);

        Attack attack = ScriptableObject.CreateInstance<Attack>();
        attack.AttackMade = bestAttInfo;
        attack.Source = _player;
        attack.Target = GameState.ListOfPlayers.Players[_player.EnemyId];
        return attack;
    }

    private AttackInfo ChooseBestAttackInfo(LogicState state, Genome genome)
    {
        var possibleMoves = state.GenerateChildren(_player.Id, _player.Attacks);
        AttackInfo bestAttack = _player.Attacks[0];
        float bestScore = float.MinValue;

        foreach (var move in possibleMoves)
        {
            AttackInfo att = move.Item1;
            LogicState nextState = move.Item2;

            float score =
                (nextState.HitPoints[_player.Id] * genome.Weights[0]) +
                (nextState.HitPoints[_player.EnemyId] * genome.Weights[1]) +
                (nextState.Energies[_player.Id] * genome.Weights[2]) +
                (att.MaxDam * att.HitChance * genome.Weights[3]) +
                (att.Energy * genome.Weights[4]);

            if (score > bestScore)
            {
                bestScore = score;
                bestAttack = att;
            }
        }

        return bestAttack;
    }

    private bool IsMatchOver(LogicState state)
    {
        return state.HitPoints.Any(hp => hp <= 0);
    }

    private LogicState SimulateAttack(LogicState state, AttackInfo attack, int attackerId)
    {
        int targetId = (attackerId + 1) % state.NumPlayers;
        float[] newHps = (float[])state.HitPoints.Clone();
        float[] newEnergies = (float[])state.Energies.Clone();

        if (newEnergies[attackerId] >= attack.Energy)
        {
            newEnergies[attackerId] -= attack.Energy;
            float expectedDamage = ((attack.MinDam + attack.MaxDam) / 2f) * attack.HitChance;
            newHps[targetId] -= expectedDamage;
        }

        return new LogicState(state, new float[state.NumPlayers], new float[state.NumPlayers])
        {
            HitPoints = newHps,
            Energies = newEnergies
        };
    }
}