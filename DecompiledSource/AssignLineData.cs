using UnityEngine;

public class AssignLineData
{
	public Vector3 startPos;

	public Vector3 endPos;

	public AssignType lineType;

	public AssignLineStatus lineStatus;

	public AssignLineData(Vector3 start_pos, Vector3 end_pos, AssignType line_type, AssignLineStatus line_status)
	{
		startPos = start_pos;
		endPos = end_pos;
		lineType = line_type;
		lineStatus = line_status;
	}
}
