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
    Stationed, 
    Working,
    Patrolling,
    Attacking
}


public enum StationType
{
    Watchtower, 
    Fortress,    
    Mine      
}