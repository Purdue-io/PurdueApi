class Meeting {
	public MeetingId: string;
	public Type: string;
	public StartDate: string;
	public EndDate: string;
	public StartTime: string;
	public Duration: string;
	public DaysOfWeek: string;
	public Instructors: Array<Instructor>;
	public Room: Room;
}