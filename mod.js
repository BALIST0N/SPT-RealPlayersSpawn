"use strict";

Object.defineProperty(exports, "__esModule", { value: true });
const fs = require('fs');

class RealPlayersSpawns 
{   
    preSptLoad(container)
    {

        let pmcConfig = container.resolve("ConfigServer").getConfig("spt-pmc");

        //disable pmc chance convertion
        pmcConfig.convertIntoPmcChance = { "default" : {"assault": {"min": 0,"max": 0} } }

        pmcConfig.looseWeaponInBackpackChancePercent = 0;
        pmcConfig.maxBackpackLootTotalRub = [{"min": 1,"max": 99,"value": 0}];
        pmcConfig.forceHealingItemsIntoSecure = false;

    }

    postDBLoad(container) 
    {
        const bots =  container.resolve("DatabaseServer").getTables().bots.types;
        let allowed_plates = ["656f57dc27aed95beb08f628","654a4dea7c17dec2f50cc86a","656fac30c6baea13cd07e10c","656f9d5900d62bcd2e02407c"]

        for(let botType in bots)
        {    
            //remove all meds, foods, drinks and nades from standards bots
            //also remove excessive loot from sptpmc
            switch(botType)
            {
                case "marksman" :
                case "arenafighter" :
                case "assault" :
                case "crazyassaultevent" :
                case "arenafighterevent" :
                case "cursedassault" :
                    bots[botType].generation.items.healing.weights = {"0": 1};
                    bots[botType].generation.items.drugs.weights = {"0": 1,};
                    bots[botType].generation.items.stims.weights = {"0": 1,};
                    bots[botType].generation.items.grenades.weights = {"0": 99,"1": 1};

                    bots[botType].generation.items.vestLoot ??= {"weights": {"0": 99,"1": 1} };
                    bots[botType].generation.items.vestLoot.weights =   {"0": 99,"1": 1};

                    bots[botType].generation.items.drink ??= {"weights": {"0": 1} };
                    bots[botType].generation.items.drink.weights =   {"0": 1};

                    bots[botType].generation.items.food ??= {"weights": {"0": 1} };
                    bots[botType].generation.items.food.weights =   {"0": 1};

                    bots[botType].generation.items.pocketLoot ??= {"weights": {"0": 90,"1": 10} };
                    bots[botType].generation.items.pocketLoot.weights = {"0": 90,"1": 10};

                    bots[botType].generation.items.currency ??= {"weights": {"0": 99,"1": 1} };
                    bots[botType].generation.items.currency.weights = {"0": 99,"1": 1};

                    bots[botType].generation.items.backpackLoot ??= {"weights": {"0": 80,"1": 10,"2":8,"3":2} };
                    bots[botType].generation.items.backpackLoot.weights = {"0": 80,"1": 10,"2":8,"3":2};

                    bots[botType].chances.equipment.ArmorVest = 20;

                    //remove plates > class 3
                    for(let itemModded in bots[botType].inventory.mods )
                    {
                        let a = bots[botType].inventory.mods[itemModded]["Front_plate"];
                        if( a !== undefined )
                        {
                            bots[botType].inventory.mods[itemModded]["Front_plate"] = a.filter(fp => allowed_plates.includes(fp))
                        } 

                        a = bots[botType].inventory.mods[itemModded]["Back_plate"];
                        if( a !== undefined )
                        {
                            bots[botType].inventory.mods[itemModded]["Back_plate"] = a.filter(bp => allowed_plates.includes(bp))
                        } 
                    }

                break;


                case "bear":
                case "usec":
                case "pmcBEAR":
                case "pmcUSEC":
                    bots[botType].generation.items.healing.weights = {"0": 80,"1": 20};
                    bots[botType].generation.items.drugs.weights = {"0": 80,"1": 20};
                    bots[botType].generation.items.stims.weights = {"0": 95,"1": 5};
                    bots[botType].generation.items.vestLoot.weights = {"0": 99,"1": 1};
                    bots[botType].generation.items.pocketLoot.weights = {"0": 90,"1": 10};
                    bots[botType].generation.items.grenades.weights = {"0": 70,"1": 20,"2": 8,"3": 2}
                    bots[botType].generation.items.backpackLoot.weights = {"0": 1};
                    bots[botType].generation.items.food.weights =   {"0": 1};
                    bots[botType].generation.items.drink.weights =   {"0": 1};
                    bots[botType].generation.items.currency.weights = {"0": 90,"1": 10};
                    bots[botType].chances.equipment.Backpack = 10;
                    bots[botType].inventory.equipment.SecondPrimaryWeapon = {};

                break;

            }
            
    }
}

module.exports = { mod: new RealPlayersSpawns() };