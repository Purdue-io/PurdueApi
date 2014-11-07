class Demo {
	private odata: OData;
	private headerStack: Array<string>;
	private elementHeader: HTMLElement;
	private elementContent: HTMLElement;
	private selectedTerm: Term;
	private selectedSubject: Subject;
	private selectedCourse: Course;

	constructor() {
		this.odata = new OData();
		this.headerStack = new Array<string>();
		this.elementHeader = (<HTMLElement>document.querySelector("nav h1"));
		document.querySelector("nav button").addEventListener("click", () => {
			this.goBack();
		});
		this.elementContent = <HTMLElement>document.querySelector("div.content");
		this.loadTerms();
	}

	public loadTerms() {
		this.odata.fetchTerms().done((data) => {
			var list = document.createElement("ul");
			list.classList.add("terms");
			for (var i = 0; i < data.length; i++) {
				((term: Term) => {
					var item = document.createElement("li");
					item.innerHTML = term.Name;
					item.addEventListener("click", () => {
						this.selectedTerm = term;
						this.loadSubjects();
					});
					list.appendChild(item);
				})(data[i]);
			}
			this.elementContent.appendChild(list);
		});
	}

	public loadSubjects() {
		this.setHeader(this.selectedTerm.Name);
		this.odata.fetchSubjects().done((data) => {
			var list = document.createElement("ul");
			list.classList.add("subjects");
			for (var i = 0; i < data.length; i++) {
				((subject: Subject) => {
					var item = document.createElement("li");
					item.innerHTML = '<span class="abbreviation">' + subject.Abbreviation + '</span>&nbsp;' + subject.Name;
					item.addEventListener("click", () => {
						this.selectedSubject = subject;
						this.loadCourses();
					});
					list.appendChild(item);
				})(data[i]);
			}
			this.elementContent.appendChild(list);
		});
	}

	public loadCourses() {
		this.setHeader(this.selectedSubject.Name);
		this.odata.fetchCourses(this.selectedTerm,this.selectedSubject).done((data) => {
			var list = document.createElement("ul");
			list.classList.add("courses");
			for (var i = 0; i < data.length; i++) {
				((course: Course) => {
					var item = document.createElement("li");
					item.innerHTML = '<span class="abbreviation">' + this.selectedSubject.Abbreviation + course.Number + '</span>&nbsp;' + course.Title;
					item.addEventListener("click", () => {
						this.selectedCourse = course;
						this.loadClasses();
					});
					list.appendChild(item);
				})(data[i]);
			}
			this.elementContent.appendChild(list);
		});
	}

	public loadClasses() {
		this.setHeader(this.selectedCourse.Title);
		this.odata.fetchClasses(this.selectedTerm, this.selectedCourse).done((data) => {
			var list = document.createElement("ul");
			list.classList.add("classes");
			for (var i = 0; i < data.length; i++) {
				((theClass: Class) => {
					var item = document.createElement("li");
					var inner = '<h1>Class ' + (i + 1) + '</h1>';
					inner += '<h2>' + theClass.Sections.length + ' section';
					if (theClass.Sections.length > 1) inner += 's';
					inner += '</h2>';
					inner += '<table><tbody>'
					inner += '<tr><th>CRN</th><th>Type</th><th>Day</th><th>Time</th></tr>';
					for (var j = 0; j < theClass.Sections.length; j++) {
						inner += '<tr><td>' + theClass.Sections[j].CRN + '</td><td>' + theClass.Sections[j].Type + '</td><td>' + theClass.Sections[j].Meetings[0].DaysOfWeek + '</td><td>' + ('0' + new Date(theClass.Sections[j].Meetings[0].StartTime).getUTCHours()).slice(-2) + ":" + ('0' + new Date(theClass.Sections[j].Meetings[0].StartTime).getUTCMinutes()).slice(-2) + '</td></tr>';
					}
					inner += '</tbody></table>';
					item.innerHTML = inner;
					list.appendChild(item);
				})(data[i]);
			}
			this.elementContent.appendChild(list);
		});
	}

	public goBack(): void {
		this.backHeader();
		var lists = this.elementContent.getElementsByTagName("ul");
		if (lists.length > 1) {
			(<HTMLElement>lists[lists.length - 1]).parentElement.removeChild(lists[lists.length - 1]);
		}
	}

	private backHeader(): void {
		if (this.headerStack.length > 0) {
			this.elementHeader.innerHTML = this.headerStack.pop();
		}
	}

	private setHeader(header: string): void {
		this.headerStack.push(this.elementHeader.innerHTML);
		this.elementHeader.innerHTML = header;
	}
}