public class ToxicMold : Plant
{
	public override void Init(bool during_load = false)
	{
		SetMesh();
		effectArea.radius = GetRadius();
		base.Init(during_load);
	}
}
