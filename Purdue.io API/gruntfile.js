/// <binding AfterBuild='default' />
/*
This file in the main entry point for defining grunt tasks and using grunt plugins.
Click here to learn more. http://go.microsoft.com/fwlink/?LinkID=513275&clcid=0x409
*/
module.exports = function (grunt) {
    grunt.initConfig({
        pkg: grunt.file.readJSON('package.json'),
        ts: {
            default: {
                src: ["Scripts/ts/**/*.ts"],
                out: "Content/script/app.js"
            }
        },
        less: {
            default: {
                files: {
                    "Content/css/style.css": "Content/css/Demo.less"
                }
            }
        },
        copy: {
            default: {
                files: [
                    {expand: true, flatten: true, src: ['Scripts/js/*'], dest: 'Content/script/', filter: 'isFile'},
                ]
            }
        }
    });

    grunt.loadNpmTasks('grunt-ts');
    grunt.loadNpmTasks('grunt-contrib-less');
    grunt.loadNpmTasks('grunt-contrib-copy');


    grunt.registerTask('default', ['less', 'ts', 'copy']);
};