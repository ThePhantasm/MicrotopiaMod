using UnityEngine;

public struct Circle
{
	public Vector3 pos;

	public float radius;

	public float radiusSq;

	public Circle(Vector3 pos, float radius)
	{
		this.pos = pos;
		this.radius = radius;
		radiusSq = radius * radius;
	}

	public Vector3 GetRandomPos()
	{
		Vector2 insideUnitCircle = Random.insideUnitCircle;
		return new Vector3(pos.x + insideUnitCircle.x * radius, 0f, pos.z + insideUnitCircle.y * radius);
	}

	public Circle(Circle c1, Circle c2)
	{
		if (c1.radius > c2.radius)
		{
			ref Vector3 reference = ref c1.pos;
			ref Vector3 reference2 = ref c2.pos;
			Vector3 vector = c2.pos;
			Vector3 vector2 = c1.pos;
			reference = vector;
			reference2 = vector2;
			ref float reference3 = ref c1.radius;
			ref float reference4 = ref c2.radius;
			float num = c2.radius;
			float num2 = c1.radius;
			reference3 = num;
			reference4 = num2;
		}
		float magnitude = (c1.pos - c2.pos).magnitude;
		if (magnitude + c1.radius < c2.radius)
		{
			pos = c2.pos;
			radius = c2.radius;
		}
		else
		{
			radius = (magnitude + c1.radius + c2.radius) / 2f;
			pos = Vector3.Lerp(c1.pos, c2.pos, 0.5f + (c2.radius - c1.radius) / (2f * magnitude));
		}
		radiusSq = radius * radius;
	}
}
