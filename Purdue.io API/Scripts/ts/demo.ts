class Demo {
	private odata: OData;
	private elementContent: HTMLElement;
	private selectedTerm: Term;
	private selectedSubject: Subject;
	private selectedCourse: Course;

	constructor() {
		this.odata = new OData();
		this.elementContent = <HTMLUListElement>document.querySelector("div.content");
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
					list.addEventListener("click", () => {
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
		this.odata.fetchSubjects().done((data) => {
			var list = document.createElement("ul");
			list.classList.add("subjects");
			for (var i = 0; i < data.length; i++) {
				((subject: Subject) => {
					var item = document.createElement("li");
					item.innerHTML = '<span class="abbreviation">' + subject.Abbreviation + '</span>&nbsp;' + subject.Name;
					list.addEventListener("click", () => {
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
		this.odata.fetchCourses(this.selectedTerm,this.selectedSubject).done((data) => {
			var list = document.createElement("ul");
			list.classList.add("courses");
			for (var i = 0; i < data.length; i++) {
				((course: Course) => {
					var item = document.createElement("li");
					item.innerHTML = '<span class="abbreviation">' + this.selectedSubject.Abbreviation + course.Number + '</span>&nbsp;' + course.Title;
					list.addEventListener("click", () => {
						this.selectedCourse = course;
						//this.loadClasses();
					});
					list.appendChild(item);
				})(data[i]);
			}
			this.elementContent.appendChild(list);
		});
	}
}