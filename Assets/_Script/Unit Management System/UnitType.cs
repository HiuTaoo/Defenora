using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum UnitType
{
    Archer,    
    Warrior,   
    Builder, 
    Lancer,
    Monk
}

public enum UnitState
{
    Idle,      
    Moving,    
    Defending, 
    Working,
    Patrolling,
    Attacking
}