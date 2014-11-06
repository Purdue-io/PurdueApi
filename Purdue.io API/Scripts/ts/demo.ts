class Demo {
	private odata: OData;
	private elementContent: HTMLElement;

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
				var item = document.createElement("li");
				item.innerHTML = data[i].Name;
				list.appendChild(item);
			}
			this.elementContent.appendChild(list);
		});
	}
}