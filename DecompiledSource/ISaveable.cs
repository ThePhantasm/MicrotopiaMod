public interface ISaveable
{
	int linkId { get; set; }

	void Write(Save save);
}
