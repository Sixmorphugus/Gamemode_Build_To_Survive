function findItemByName(%item)
{
	for(%i=0;%i<DatablockGroup.getCount();%i++)
	{
		%obj = DatablockGroup.getObject(%i);
		if(%obj.getClassName() $= "ItemData")
			if(strPos(%obj.uiName,%item) >= 0)
				return %obj.getName();
	}
	return -1;
}

function findLightByName(%item)
{
	for(%i=0;%i<DatablockGroup.getCount();%i++)
	{
		%obj = DatablockGroup.getObject(%i);
		if(%obj.getClassName() $= "fxLightData")
			if(strPos(%obj.uiName,%item) >= 0)
				return %obj.getName();
	}
	return -1;
}

function findEmitterByName(%item)
{
	for(%i=0;%i<DatablockGroup.getCount();%i++)
	{
		%obj = DatablockGroup.getObject(%i);
		if(%obj.getClassName() $= "ParticleEmitterData")
			if(strPos(%obj.uiName,%item) >= 0)
				return %obj.getName();
	}
	return -1;
}

function Player::addNewItem(%player,%item)
{
	%client = %player.client;
	if(isObject(%item))
	{
		if(%item.getClassName() !$= "ItemData") return -1;
		%item = %item.getName();
	}
	else
		%item = findItemByName(%item);
	if(!isObject(%item)) return;
	%item = nameToID(%item);
	for(%i = 0; %i < %player.getDatablock().maxTools; %i++)
	{
		%tool = %player.tool[%i];
		if(!isObject(%tool))
		{
			%player.tool[%i] = %item;
			%player.weaponCount++;
			messageClient(%client,'MsgItemPickup','',%i,%item);
			break;
		}
	}
}