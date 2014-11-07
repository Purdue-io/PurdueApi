class OData {
	private static SERVICE_ENDPOINT: string = "/odata";

	public fetchTerms(): JQueryPromise<Array<Term>> {
		var retval: JQueryDeferred<any> = $.Deferred<any>();
		$.getJSON(OData.SERVICE_ENDPOINT + "/Terms")
			.done(function (data) {
				retval.resolve(<Array<Term>>data.value);
			}).fail(function () {
					retval.reject();
			});
		return retval;
	}

	public fetchSubjects(): JQueryPromise<Array<Subject>> {
		var retval: JQueryDeferred<any> = $.Deferred<any>();
		$.getJSON(OData.SERVICE_ENDPOINT + "/Subjects?$orderby=Abbreviation%20asc")
			.done(function (data) {
				retval.resolve(<Array<Term>>data.value);
			}).fail(function () {
				retval.reject();
			});
		return retval;
	}

	public fetchCourses(term: Term, subject: Subject): JQueryPromise<Array<Course>> {
		var retval: JQueryDeferred<Array<Course>> = $.Deferred<Array<Course>>();
		$.getJSON(OData.SERVICE_ENDPOINT + "/Courses?$filter=Classes/any(c:%20c/Term/TermCode%20eq%20'" + term.TermCode + "')%20and%20Subject/Abbreviation%20eq%20'" + subject.Abbreviation + "'&$orderby=Number%20asc")
			.done(function (data) {
				retval.resolve(data.value);
			})
			.fail(function () {
				retval.reject();
			});
		return retval;
	}

	public fetchClasses(term: Term, course: Course): JQueryPromise<Array<Class>> {
		var retval: JQueryDeferred<Array<Class>> = $.Deferred<Array<Class>>();
		var query = "/Classes?$expand=Sections($expand=Meetings($expand=Instructors,Room($expand=Building)))&$filter=Course/CourseId%20eq%20" + course.CourseId + "%20and%20Term/TermId%20eq%20" + term.TermId;
		$.getJSON(OData.SERVICE_ENDPOINT + query)
			.done(function (data) {
				retval.resolve(data.value);
			})
			.fail(function () {
				retval.reject();
			});
		return retval;
	}
}