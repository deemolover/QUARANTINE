﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public enum VarType
{
    HEALTHY_POP,
    INFECTED_POP_CURR_GEN,
    INFECTED_POP_NEXT_GEN,
    MATERIAL,
    NONE
}

public enum BlockType
{
    ERROR,
    FACTORY,
    HOUSING,
    HOSPITAL,
    QUARANTINE
}

public class Utility
{
    public static int AdaptedRandomNumber(float ratio, int total)
    {
        int part = (int)(Random.Range(0, ratio) * total);
        return part;
    }
}

/// <summary>
/// Variable is a basic data structure defined to cope with batch iteration.
/// It's basically a piece of data, and can be read and modified as common value type.
/// <example>
///    This example will read data out of a Variable, increase it by 1, and write back
///    <code>
///        Variable<int> p(2, VarType.NONE); // data initialized to 2
///        int a = p.Data; // read out
///        a += 1;
///        p.Data = a; // write back
///    </code>
///    Of course you can simplify the code like this:
///    <code>
///        Variable<int> p(2, VarType.NONE); // data initialized to 2
///        p.Data += 1;
///    </code>
/// </example>
/// But there is some time you have to deal with a batch of variables, and you may want to 
/// iterate on them, new value of each depending on old values of some. You can create a 
/// buffer, calculate new values and store them to the buffer during computation, and 
/// assign the buffer to variables after the computation is completed. But when this kind 
/// of operation becomes frequent, the whole process will be annoying. So how about binding 
/// the buffer directly to the variable? This is how the Variable class work. You can write
/// to its DataBuf field while keeping its Data value unchanged, and call Commmit() to 
/// update the value of Data. Plus, Writing to Data field will cause value in DataBuf to 
/// follow, but it's not recommended to read DataBuf unless you are clear about how this field
/// is handled. Here are some examples:
/// <example>
/// the following code will calculate Fibonacci value when iterated.
///     <code>
///         Variable<int> a(1, VarType.NONE);
///         Variable<int> b(0, VarType.NONE);
///         for (int i=0;i<10;++i){
///             a.DataBuf = a.Data + b.Data;
///             b.DataBuf = a.Data;
///             a.Commit(); b.Commit();
///         }
///     </code>
/// </example>
/// </summary>
/// <typeparam name="T"></typeparam>
public class Variable<T> where T: struct
{
    private T data;
    private T dataBuffer;
    private VarType type;
    private bool dirty;

    public T Data
    {
        get
        {
            return data;
        }
        set
        {
            data = value;
            dataBuffer = value;
            dirty = false;
        }
    }
    public T DataBuf
    {
        get
        {
            return dataBuffer;
        }
        set
        {
            dataBuffer = value;
            dirty = true;
        }
    }


    public float priority;
    public List<Variable<T>> outVariables;
    public static readonly float ZERO = 1e-6f;
    
    public Variable(T mData, VarType mType)
    {
        data = mData;
        type = mType;
        dirty = false;
        priority = 0.0f;
        outVariables = new List<Variable<T>>();
    }

    public bool NeedCommit()
    {
        return dirty;
    }
    public void Commit()
    {
        data = dataBuffer;
        dirty = false;
    }

    public void AddOutVariable(Variable<T> var)
    {
        outVariables.Add(var);
    }

    public virtual void Broadcast(float ratio, float offset = 0.0f) { }
}

public class BroadcastVariableInt : Variable<int>
{
    public BroadcastVariableInt(int mData, VarType mType): base(mData, mType) { }

    /// <summary>
    /// Method used for data exchanging between a variable and those of its vicinity 
    /// (stored in outVariables defined in Variable)
    /// </summary>
    /// <param name="ratio">ratio by which Data of this variable will be distributed.
    /// for example, "ratio=0.5f" means that half the Data will be given out.</param>
    /// <param name="offset"></param>
    public override void Broadcast(float ratio, float offset = 0.0f)
    {
        List<Variable<int>> finalOuts = new List<Variable<int>>();
        float prioritySum = 0.0f;
        foreach (Variable<int> ele in outVariables)
        {
            if (ele.priority < offset) // will be ignored
            {
                continue;
            }
            prioritySum += ele.priority + 1;
            finalOuts.Add(ele);
        }

        if (prioritySum < ZERO) return;

        if (ratio <= ZERO) ratio = 0.0f;
        int delta = (int)(Data * ratio);
        if (Data < delta) delta = Data;
        int outSum = delta;
        foreach (Variable<int> ele in finalOuts)
        {
            int alloc = (int)(delta * ((ele.priority+1) / prioritySum));
            if (outSum < alloc) alloc = outSum;
            ele.DataBuf += alloc;
            outSum -= alloc;
        }
        DataBuf -= delta - outSum;
    }
}

public class InfectedStage
{
    public static readonly InfectedStage[] stages = { 
        new InfectedStage(0.0f, 0.0f),
        new InfectedStage(1.0f, 0.0f),
        new InfectedStage(1.0f, 0.1f),
        new InfectedStage(1.0f, 0.5f)
    };
    public float reproduction; // R0
    public float deathRate;
    public InfectedStage(float mReproduction, float mDeathRate){
        reproduction = mReproduction;
        deathRate = mDeathRate;
    }
}

public class InfectedTimer
{
    List<InfectedStage> stages;
    public readonly int period; // T = 3D
    public int timer;
    public int stagePointer;
    public InfectedTimer(int mPeriod)
    {
        stages = new List<InfectedStage>();
        period = mPeriod;
        // stagePointer = mInitStage % InfectedStage.stages.Length;
        stagePointer = 0;
    }

    public int AddStage(InfectedStage stage)
    {
        stages.Add(stage);
        return 0;
    }

    /* will return 1 if a stage cycle complete */
    public int Tick()
    {
        if (stages.Count == 0)
            return 0;
        timer++;
        if (timer == period)
        {
            timer = 0;
            stagePointer = (stagePointer+1) % stages.Count;
            if (stagePointer == 0)
                return 1;
        }

        return 0;
    }

    public float Reproduction
    {
        get
        {
            return stages[stagePointer].reproduction;
        }
    }

    public float DeathRate
    {
        get
        {
            return stages[stagePointer].deathRate;
        }
    }
    
}


/* BlockTypeParameter
 * important parameters that are strongly (or at least potentially) related to block type
 * though some of them may be temporarily the same for all types of blocks
 */
public class BlockTypeParameter
{
    public static readonly BlockTypeParameter factory = new BlockTypeParameter(BlockType.FACTORY);
    public static readonly BlockTypeParameter housing = new BlockTypeParameter(BlockType.HOUSING);
    public static readonly BlockTypeParameter hospital = new BlockTypeParameter(BlockType.HOSPITAL);
    public static readonly BlockTypeParameter quarantine = new BlockTypeParameter(BlockType.QUARANTINE);

    public BlockType type;
    
    public readonly float R0 = 2.0f;
    public readonly float DR = 1.0f;

    public readonly float MCR = -0.01f; // material count rate
    public readonly float WMCR = +1f;   // material count rate FOR FACTORY

    public readonly int RESOURCE_MIN = 0;
    public readonly float TAX_RATE = 0.05f;

    public BlockTypeParameter(BlockType mType)
    {
        type = mType;
        if (type == BlockType.HOSPITAL)
        {
            R0 = 0.5f;
            DR = 0.0f;
            MCR = -0.1f;
        }
    }

}

public class Block
{
    public BlockTypeParameter parameter;
    public BlockType type
    {
        get
        {
            if (parameter == null)
                return BlockType.ERROR;
            return parameter.type;
        }

        set
        {
            switch (value)
            {
                case BlockType.FACTORY:
                    {
                        parameter = BlockTypeParameter.factory;
                        break;
                    }
                case BlockType.HOUSING:
                    {
                        parameter = BlockTypeParameter.housing;
                        break;
                    }
                case BlockType.HOSPITAL:
                    {
                        parameter = BlockTypeParameter.hospital;
                        break;
                    }
                case BlockType.QUARANTINE:
                    {
                        parameter = BlockTypeParameter.quarantine;
                        break;
                    }
                default:
                    {
                        parameter = null;
                        break;
                    }
            }
        }
    }
    public GameObject blockUI;
    public List<Block> outBlocks; // Blocks

    /* material related */

    public bool isWorking = false;
    public float MCRate
    {
        get
        {
            if (isWorking) return parameter.WMCR;
            else return parameter.MCR;
        }

    }

    public int RESOURCE_MIN
    {
        get
        {
            return parameter.RESOURCE_MIN;
        }
    }
    public float taxRate
    {
        get
        {
            return parameter.TAX_RATE;
        }
    }

    /* population related */
    InfectedTimer CTimer = new InfectedTimer(3); // Current generation counter
    InfectedTimer NTimer = new InfectedTimer(3); // Next generation counter
    public float CR0
    {
        get
        {
            return parameter.R0 * CTimer.Reproduction;
        }
    }
    public float NR0
    {
        get
        {
            return parameter.R0 * NTimer.Reproduction;
        }
    }
    public float CDR
    {
        get
        {
            return parameter.DR * CTimer.DeathRate;
        }
    }
    public float NDR
    {
        get
        {
            return parameter.DR * NTimer.DeathRate;
        }
    }

    public Variable<int> HPCount;
    public Variable<int> CIPCount; // Current generation of infected
    public Variable<int> NIPCount; // Next generation of infected, temporarily not used

    public Variable<int> MaterialCount;

    public void Commit()
    {
        HPCount.Commit();
        CIPCount.Commit();
        NIPCount.Commit();
        MaterialCount.Commit();
    }

    public Block(GameObject mBlockUI)
    {
        blockUI = mBlockUI;
        Landblock lb = blockUI.GetComponent<Landblock>();
        type = lb.type;
        
        lb.block = this;
        outBlocks = new List<Block>();

        NTimer.AddStage(InfectedStage.stages[0]);
        NTimer.AddStage(InfectedStage.stages[1]);
        CTimer.AddStage(InfectedStage.stages[2]);
        CTimer.AddStage(InfectedStage.stages[3]);

        /* deprecated
        HPCount = new BroadcastVariableInt(0, VarType.HEALTHY_POP);
        CIPCount = new BroadcastVariableInt(0, VarType.INFECTED_POP_CURR_GEN);
        NIPCount = new BroadcastVariableInt(0, VarType.INFECTED_POP_NEXT_GEN);

        MaterialCount = new Variable<int>(0, VarType.MATERIAL);

        // initialize infected population
        HPIPInit(lb.infected);
        MCInit(lb.material);
        */

        VarIntInit(ref HPCount, Random.Range(400, 600), VarType.HEALTHY_POP);
        VarIntInit(ref CIPCount, 0, VarType.INFECTED_POP_CURR_GEN);
        VarIntInit(ref NIPCount, lb.infected, VarType.INFECTED_POP_NEXT_GEN);
        VarIntInit(ref MaterialCount, lb.material, VarType.MATERIAL);

        /* block type specific */
        if (type == BlockType.FACTORY)
            isWorking = true;

        if (type == BlockType.HOSPITAL)
            CIPCount.priority = 20.0f;

    }

    public void AddOutBlock(Block target)
    {
        outBlocks.Add(target);
        HPCount.AddOutVariable(target.HPCount);
        CIPCount.AddOutVariable(target.CIPCount);
        NIPCount.AddOutVariable(target.NIPCount);
        MaterialCount.AddOutVariable(target.MaterialCount);

    }

    public void VarIntInit(ref Variable<int> variable, int data, VarType type)
    {
        variable = new BroadcastVariableInt(data, type);
        foreach (Block block in outBlocks)
        {
            switch (type)
            {
                case VarType.HEALTHY_POP:
                    {
                        variable.AddOutVariable(block.HPCount);
                        break;
                    }
                case VarType.INFECTED_POP_CURR_GEN:
                    {
                        variable.AddOutVariable(block.CIPCount);
                        break;
                    }
                case VarType.INFECTED_POP_NEXT_GEN:
                    {
                        variable.AddOutVariable(block.NIPCount);
                        break;
                    }
                case VarType.MATERIAL:
                    {
                        variable.AddOutVariable(block.MaterialCount);
                        break;
                    }
                default: break;
            }
        }
    }

    /* deprecated
    public void HPIPInit(int infected)
    {
        HPCount.Data = Random.Range(400, 600);
        CIPCount.Data = 0;
        NIPCount.Data = infected;
    }
    public void MCInit(int material)
    {
        MaterialCount.Data = material;
    }
    */

    /* 以下方法段是一些与父对象互动的工具方法 */
    public int StopWorking()
    {
        isWorking = false;
        return 0;
    }
    public int StartWorking()
    {
        if (type == BlockType.FACTORY)
        {
            isWorking = true;
            return 0;
        } else
        {
            return 1;
        }
    }

    public int Taxed()
    {
        int taxed = (int)System.Math.Floor(MaterialCount.Data * taxRate);
        MaterialCount.Data -= taxed;
        return taxed;
    }

    int QUARANTINE_PERIOD = 10;
    bool isQuarantined;
    int quarantineCounter;
    public int Quarantined(int period)
    {
        isQuarantined = true;
        QUARANTINE_PERIOD = period;
        quarantineCounter = 0;
        return 0;
    }

    public int Aided()
    {
        HPCount.Data = 0;
        CIPCount.Data = 0;
        NIPCount.Data = 0;
        return 0;
    }

    /* 回合结束时首先执行的块内结算，不涉及与其他块的数据交换 */
    public void EndInBlock()
    {
        // population counted
        int develop = CTimer.Tick() + NTimer.Tick();
        if (develop > 0) // pandemics developed
        {
            // shift generation
            HPCount.Data += CIPCount.Data;
            CIPCount.Data = NIPCount.Data;
            NIPCount.Data = 0;
        }
        int inf = (int)(CIPCount.Data * CR0 + NIPCount.Data * NR0);
        if (inf > HPCount.Data) inf = HPCount.Data;
        NIPCount.Data += inf;
        HPCount.Data -= inf;

        int death = Utility.AdaptedRandomNumber(CDR, CIPCount.Data);
        CIPCount.Data -= death;
        death = Utility.AdaptedRandomNumber(NDR, NIPCount.Data);
        NIPCount.Data -= death;

        // material counted
        MaterialCount.Data += (int)System.Math.Floor(MCRate*(HPCount.Data+CIPCount.Data));
        if (MaterialCount.Data < RESOURCE_MIN)
            MaterialCount.Data = RESOURCE_MIN;
    }

    /* 这一阶段涉及的数据计算一般需要依赖于来自其他区块的数据 */
    public void EndRound()
    {
        if (isQuarantined)
        {
            quarantineCounter += 1;
            if (quarantineCounter == QUARANTINE_PERIOD)
            {
                isQuarantined = false;
            }
            return;
        }

        /*
        int len = outBlocks.Count;
        int distr = HPCount.Data / len / 2;
        int cdistr = CIPCount.Data / len / 2;
        int ndistr = NIPCount.Data / len / 2;
        foreach (Block target in outBlocks)
        {
            target.HPCount.DataBuf += distr;
            HPCount.DataBuf -= distr;

            target.CIPCount.DataBuf += cdistr;
            CIPCount.DataBuf -= cdistr;

            target.NIPCount.DataBuf += ndistr;
            NIPCount.DataBuf -= ndistr;
        }
        */

        HPCount.Broadcast(0.5f);
        if (type != BlockType.HOSPITAL)
            CIPCount.Broadcast(0.9f);
        else
            CIPCount.Broadcast(0.9f, 2.0f);
        NIPCount.Broadcast(0.5f);
        MaterialCount.Broadcast(0.5f);
    }
}