public sealed class PlayerDresser : Component, Component.INetworkSpawn
{
	[Property]
	public SkinnedModelRenderer BodyRenderer { get; set; }

	public void OnNetworkSpawn( Connection owner ) => ClothingContainer.CreateFromJson( owner.GetUserData( "avatar" ) ).Apply( BodyRenderer );

}
