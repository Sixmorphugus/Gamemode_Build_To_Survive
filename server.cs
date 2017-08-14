if( $GameModeArg !$= "Add-Ons/GameMode_Build_To_Survive/gamemode.txt" )
{
	error( "Error: GameMode_Build_To_Survive cannot be used in custom games! Please use the Script version of the mod." );
	return;
}

$Disasters::AllowExt = true;

exec("./server/support.cs");
exec("./server/autosaver.cs");
exec("./server/wrench.cs");
exec("./server/admin.cs");
exec("./server/main.cs");

function serverCmdHelp(%cl) {
    MessageClient(%cl, '', "\c2Disasters is basically survival.");
    MessageClient(%cl, '', "\c2The goal is for the players to survive the chosen act of god for the entire round.");
    MessageClient(%cl, '', "\c3/checkDisasters \c2lets you see possible things that could happen.");
	MessageClient(%cl, '', "\c3/seeRecord \c2shows the current record holder.");
    MessageClient(%cl, '', "\c3/checkPlayers \c2lets you check who's alive and who's dead.");
    MessageClient(%cl, '', "\c3/upgrade [upgradename] \c2allows you to spend points on single-round power-ups.");
}

function rldf() {
	setModPaths(getModPaths());
	exec("./server.cs");
	transmitDataBlocks();
}

function rld() {
	exec("./server.cs");
}