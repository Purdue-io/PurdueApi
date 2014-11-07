class Demo {
	private odata: OData;
	private elementContent: HTMLElement;
	private termCode: string;

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
						this.termCode = term.TermCode;
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
				var item = document.createElement("li");
				item.innerHTML = '<span class="abbreviation">' + data[i].Abbreviation + '</span>&nbsp;' + data[i].Name;
				list.appendChild(item);
			}
			this.elementContent.appendChild(list);
		});
	}

	public loadCourses() {
		this.odata.fetchSubjects().done((data) => {
			var list = document.createElement("ul");
			list.classList.add("courses");
			for (var i = 0; i < data.length; i++) {
				var item = document.createElement("li");
				item.innerHTML = data[i].Name;
				list.appendChild(item);
			}
			this.elementContent.appendChild(list);
		});
	}
}