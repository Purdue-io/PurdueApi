class QueryTester {
    private elementContent: HTMLElement;
    private elementQuery: HTMLInputElement;
    private elementDropDown: HTMLSelectElement;

    constructor() {
        document.querySelector("nav button").addEventListener("click", () => {
            this.newQuery();
        });
        this.elementContent = <HTMLElement>document.querySelector("div.content");
        this.elementQuery = <HTMLInputElement>document.getElementById("queryBox");
        this.elementDropDown = <HTMLSelectElement>document.getElementById("dropDown");
    }

    public newQuery(): void {
        var xmlHttp = null;
        var query = this.elementQuery.value;

        xmlHttp = new XMLHttpRequest();
        xmlHttp.open("GET", DataSource.APIURL + query, true);
        xmlHttp.onreadystatechange = handlerFunction;
        xmlHttp.send(null);

        function handlerFunction() {
            if (xmlHttp.readyState == 4 && xmlHttp.status == 200) {
                document.getElementById("resultsBox").textContent = xmlHttp.responseText;
            }
            else {
                document.getElementById("resultsBox").textContent = xmlHttp.statusText;
            }
        }
    }

    public useExampleQuery(): void {

        switch (this.elementDropDown.selectedIndex) {
            case 1:
                this.elementQuery.value = "Courses?$filter=contains(Title, 'Algebra')";
                break;
            case 2:
                this.elementQuery.value = "Courses?$filter=Subject/Abbreviation eq 'CS'&$orderby=Number asc";
                break;
            case 3:
                this.elementQuery.value = "Courses?$filter=Subject/Abbreviation eq 'SPAN' and Number ge '30000' and Number le '39999'&$orderby=Number asc";
                break;
            case 4:
                this.elementQuery.value = "Courses?$filter=Classes/any(c: c/Sections/any(s: s/Meetings/any(m: m/Instructors/any(i: i/Name eq 'Hubert E. Dunsmore'))))";
                break;
            case 5:
                this.elementQuery.value = "Terms?$orderby=StartDate%20desc";
                break;
            case 6:
                this.elementQuery.value = "Classes?$expand=Sections($expand=Meetings)&$filter=(Sections/any(s: s/Meetings/any(m: m/Room/Building/ShortCode eq 'CL50'))) and (Sections/any(s: s/Meetings/any(m: m/Room/Number eq '224')))";
                break;
            case 7:
                this.elementQuery.value = "Courses?$filter=Subject/Abbreviation eq 'MA' and CreditHours eq 4 and (Classes/any(c: c/Sections/any(s: s/RemainingSpace gt 0))) and (Classes/any(c: c/Sections/any(s: s/Type eq 'Lecture')))";
                break;
            default:
                break;
        }
    }
}