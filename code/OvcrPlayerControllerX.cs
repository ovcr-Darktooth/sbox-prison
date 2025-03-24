using Sandbox;
using Sandbox.Citizen;
using Overcreep;
namespace XMovement;

public partial class OvcrPlayerControllerX : PlayerWalkControllerComplex
{
	[Property] public Currencies Currencies { get; set; }
	[Property] public Enchantments Enchantments { get; set; }
	[Property] public Multiplicators Multiplicators { get; set; } 
}
