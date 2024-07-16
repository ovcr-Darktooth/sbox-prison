using Sandbox;


public enum Enchants
{
    Jackhammer,
    Laser,
    Fortune,
    Efficiency
}

public sealed class Enchantments : Component
{
  

	public int Jackhammer { get; set; } = 5;
	public int Laser { get; set; } = 5;
	public int Fortune { get; set; } = 5;
	public int Efficiency { get; set; } = 5;


	protected override void OnUpdate()
	{
		
	}

	public float getDefaultChanceOfEnchant(Enchants enchantment)
	{
		switch (enchantment)
		{
			case Enchants.Jackhammer:
				return 1;
			case Enchants.Laser:
				return 1;
			case Enchants.Fortune:
				return 1;
			case Enchants.Efficiency:
				return 1;
			default:
				return 0.0f;
		} 
	}

	public float getChanceOfEnchant(Enchants enchantment)
	{
		switch (enchantment)
		{
			case Enchants.Jackhammer:
				return getDefaultChanceOfEnchant(Enchants.Jackhammer) * Jackhammer;
			case Enchants.Laser:
				return getDefaultChanceOfEnchant(Enchants.Laser) * Laser;
			case Enchants.Fortune:
				return getDefaultChanceOfEnchant(Enchants.Fortune) * Fortune;
			case Enchants.Efficiency:
				return getDefaultChanceOfEnchant(Enchants.Efficiency) * Efficiency;
			default:
				return 0.0f;
		} 
	}

	public static string GetEnchantmentText(Enchants enchantment)
	{
		switch (enchantment)
		{
			case Enchants.Jackhammer:
				return "Jackhammer";
			case Enchants.Laser:
				return "Laser";
			case Enchants.Fortune:
				return "Fortune";
			case Enchants.Efficiency:
				return "Efficiency";
			default:
				return "Enchant not valid";
		}
	}


	public float getPriceOfEnum(Enchants enchantment, int level)
	{
		switch (enchantment)
		{
			case Enchants.Jackhammer:
				return 5;
			case Enchants.Laser:
				return 5;
			case Enchants.Fortune:
				return 15;
			case Enchants.Efficiency:
				return 20;
			default:
				return 0.0f;
		} 
	}



}
