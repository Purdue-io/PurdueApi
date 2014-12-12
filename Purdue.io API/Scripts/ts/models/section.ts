class Section {
	public SectionId: string;
	public CRN: string;
	public Type: string;
	public Capacity: number;
	public Enrolled: number;
	public RemainingSpace: number;
	public WaitlistCapacity: number;
	public WaitlistCount: number;
	public WaitlistSpace: number;
	public StartDate: Date;
	public EndDate: Date;
	public Meetings: Array<Meeting>;
} 