import { Schema } from '../../ng4-schema-forms/index';

export const sampleSchema: Schema = {
    type: 'object',
    title: 'GUI:',
    description: 'a description example',
    properties: {
        // String Controls
        stringExample: {
            type: 'string',
            title: 'Name',
            description: 'a string description example',
            default: 'test', // '2017-11-11'
            placeholder: 'Name',
            validators: [
                {
                    type: 'required',
                    value: null,
                    errorMessage: null // 'The field stringExample is required'
                },
                // {
                //     type: 'pattern',
                //     value: 'w+@[a-zA-Z_]+?.[a-zA-Z]{2,3}$',
                //     errorMessage: 'Some regex error message for stringExample'
                // },
                // {
                //     type: 'custom',
                //     value: 'customStringValidation',
                //     errorMessage: 'Some custom STRING error message for stringExample - server'
                // },
                // {
                //     type: 'custom',
                //     value: 'customDateValidation', // ok with 2017-11-11
                //     errorMessage: 'Some custom DATE error message  for stringExample - server'
                // }
            ]
        },
        emailStringExample: {
            type: 'string',
            title: 'Email',
            description: 'a string description example',
            // default: 'test',
            placeholder: 'Email'
            // validators: [
            //     {
            //         type: 'required',
            //         value: null,
            //         errorMessage: 'The emailStringExample field is required'
            //     },
            //     // {
            //     //     type: 'pattern',
            //     //     value: '[A-z]{3}',
            //     //     errorMessage: 'Some error for emailStringExample message'
            //     // }
            //     // {
            //     //     type: 'custom',
            //     //     value: 'customStringValidation',
            //     //     errorMessage: 'Some custom error message 2 for emailStringExample'
            //     // }
            // ]
        },
        // Number
        number2Example: {
            type: 'number',
            title: 'A number example',
            description: 'a number description example',
            default: 7
        },
        ngPatternExample: {
            type: 'string',
            title: 'ngPattern Example',
            description: 'a string description ngPatternExample',
            default: 'abc',
        },
        stringExample2: {
            type: 'string',
            title: 'A string example 2',
            description: 'a string description example 2',
            default: 'test',
            placeholder: 'Last Name',
            minLength: 4,
            maxLength: 25,
        },
        dateExample: {
            title: 'A string date example',
            type: 'string',
            format: 'date',
            description: 'a string description dateExample',
            default: '2017-06-01',
        },
        colorExample: {
            type: 'string',
            title: 'Color Example',
            format: 'color',
            default: '#FFA500',
            description: 'a string description colorExample'
        },
        enumExample: {
            type: 'string',
            title: 'A string enum example 2',
            enum: [
                ['One', 'enum 1'],
                ['Two', 'enum 2'],
                ['Three', 'enum 3'],
                ['Four', 'enum 4'],
                ['Five', 'enum 5'],
            ],
            default: 'enum 2',
            description: 'a string description enumExample'
        },
        enumOptionalExample: {
            type: 'string',
            title: 'A string enum optional example 2',
            placeholder: 'Select One...',
            enum: [
                ['One', 'enum 1'],
                ['Two', 'enum 2'],
                ['Three', 'enum 3'],
                ['Four', 'enum 4'],
                ['Five', 'enum 5'],
            ],
            // required: true,
            optional: true,
            description: 'a string description enumOptionalExample'
        },
        textareaExample: {
            type: 'string',
            title: 'A String Textarea Example',
            format: 'textarea',
            placeholder: 'Enter Description ...',
            default: 'Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed lacinia magna non nulla accumsan ornare. Sed convallis lectus et dolor vestibulum, non fermentum felis eleifend.',
            maxLength: 250,
            description: 'a string description textareaExample',
            required: true
        },
        readOnlyExample: {
            title: ' Read Only Example',
            type: 'string',
            readonly: true,
            default: 'abc',
            description: 'a string description readOnlyExample'
        },
        readOnlyAndOptionalExample: {
            type: 'string',
            title: 'Read Only Optional Example',
            readonly: true,
            default: 'abc',
            description: 'a string description readOnlyAndOptionalExample'
        },
        optionalExample: {
            type: 'string',
            title: 'Optional Example',
            description: 'a string description optionalExample'
        },
        optionalAndDefaultExample: {
            type: 'string',
            title: 'Optional and Default Example - d',
            default: 'abc',
            description: 'a string description optionalAndDefaultExample'
        },
        patternExample: {
            type: 'string',
            title: 'Pattern Example',
            pattern: '^[A-z]{3}$',
            default: 'abc',
            description: 'a string description patternExample'
        },
        // Boolean
        booleanExample: {  // Checkbox true/false => plus label - Default setting
            type: 'boolean',
            title: 'Default boolean example',
            description: 'Default boolean description example',
        },
        booleanRadioExample: { // Radio boxes  Not optional by default
            type: 'boolean',
            title: 'A boolean radio example',
            description: 'a boolean description example',
            format: 'radio',
        },
        booleanSelectExample: {  // Select Control true/false options plus label
            type: 'boolean',
            format: 'select',
            title: 'A boolean select example not optional',
            description: 'a boolean description example',
            default: true,
            // validators: {
            //     requiredTrue: true
            // }
        },
        booleanSelectOptionalExample: { // Select Control true/false options plus label with default option
            type: 'boolean',
            optional: true,
            title: 'A boolean select optional example',
            description: 'a boolean description example',
        },
        // Number
        integerExample: {
            type: 'integer',
            title: 'A integer example',
            description: 'a integer description example',
        },
        integerExample2: {
            type: 'number',
            title: 'A number select example',
            description: 'a number description example',
            default: 4,
            optional: true,
            enum: [
                ['One', 1],
                ['Two', 2],
                ['Three', 3],
                ['Four', 4],
                ['Five', 5],
            ],
        },
        numberExample: {
            type: 'number',
            title: 'A number example',
            description: 'a number description example',
            placeholder: 'Rate',
            default: 123.4,
        },

        // Null ***** TBD for buttons and other html content
        nullExample: {
            type: 'null',
            title: 'A Null Example',
            description: 'a null description example',
            default: null,
        },

        // Array
        pets: {
            type: 'array',
            // format: 'table',
            title: ' Pets',
            minItems: 2,
            uniqueItems: true,
            collapsed: false,
            items: {
                type: 'object',
                title: 'Pet',
                properties: {
                    type: {
                        type: 'string',
                        enum: [
                            ['cat','cat'],
                            ['dog','dog'],
                            ['bird','bird'],
                            ['reptile','reptile'],
                            ['other','other'],
                        ],
                    },
                    name: {
                        type: 'string',
                        // customValidatorKeys: 'customStringValidation'
                    }
                }
            },
            default: [
                {
                    type: 'dog',
                    name: 'Walter'
                },
                {
                    type: 'cat',
                    name: 'Samantha'
                }
            ]
        },
        arrayExample: {
            type: 'array',
            title: 'Array Editor Template',
            description: 'a array description example',
            items: {
                type: 'string',
                maxLength: 15
            },
            default: ['Arrest #1', 'Arrest #2'],
            minItems: 1,
            uniqueItems: true,
            collapsed: false
        },
        arrayNumberExample: {
            type: 'array',
            title: 'Array Number Editor Template',
            description: 'a boolean array description example',
            items: {
                type: 'boolean',
                title: 'this is a boolean label'
            },
            default: [true, false],
            collapsed: false
        },
        // Array of objects
        itemTitleExample: {
            title: 'Array Template',
            type: 'array',
            description: 'a array itemTitleExample',
            uniqueItems: true,
            collapsed: false,
            items: {
                title: 'Array of Object Template',
                type: 'object',
                properties: {
                    propertyExample1: {
                        type: 'string'
                    },
                    propertyExample2: {
                        type: 'number'
                        // multipleOf:2
                    },
                    propertyExample3: {
                        type: 'boolean',
                        title: 'Default boolean example',
                        description: 'Default boolean description example'
                    }
                },
                requiredProperties: ['propertyExample1', 'propertyExample2'],
            },
            default: [
                {
                    propertyExample1: 'foo',
                    propertyExample2: 1,
                    propertyExample3: true
                },
                {
                    propertyExample1: 'bar',
                    propertyExample2: 2,
                    propertyExample3: false
                },
                {
                    propertyExample1: 'baz',
                    propertyExample2: 3,
                },
                {
                    propertyExample1: 'abc',
                    propertyExample2: 4,
                },
                {
                    propertyExample1: 'def',
                    propertyExample2: 5,
                },
                {
                    propertyExample1: 'ghi',
                    propertyExample2: 6,
                }
            ],
        },
        // Object
        location: {
            type: 'object',
            title: ' Location',
            description: 'a object Location example',
            collapsed: false,
            properties: {
                city: {
                    type: 'string',
                    default: 'San Francisco',
                    description: 'a object properties -> City description'
                },
                state: {
                    type: 'string',
                    default: 'CA',
                    description: 'a object properties -> State description',
                    // customValidatorKeys: 'customStringValidation'
                },
                Capital: {
                    type: 'boolean',
                    description: 'a object properties -> boolean description',
                    default: true
                }
            }
        },
        // Object Validation - minProperties & maxProperties
        optionalObjectExample: {
            type: 'object',
            title: 'An Object example',
            description: 'a object optionalObjectExample',
            properties: {
                propertyExample1: {
                    type: 'string',
                    default: 'testing objects'
                },
                propertyExample2: {
                    type: 'number',
                    // default: 100
                }
            },
            maxProperties: 5,
            minProperties: 1,
            collapsed: false
        },
        // Object Default values passed separately
        ObjectExample1: {
            type: 'object',
            title: 'An object with defaults example1 ',
            description: 'an object description example',
            properties: {
                propertyExample1: {
                    type: 'string',
                    default: 'overrides obj defaults'
                },
                propertyExample2: {
                    type: 'number',
                },
                propertyExample3: {
                    type: 'string',
                },
                propertyExample4: {
                    type: 'string',
                },
                propertyExample5: {
                    type: 'string',
                },
            },
            default: [{
                propertyExample1: 'test',
                propertyExample2: 5,
                propertyExample3: 'testing defaults',
                propertyExample4: 'testing defaults 4',
                propertyExample5: 'testing defaults 5'
            }],
            requiredProperties: ['propertyExample1', 'propertyExample2'],
            collapsed: false
        },
        collapsedObjectExample2: {
            type: 'object',
            title: 'CollapsedObjectExample',
            description: 'a object collapsedObjectExample',
            properties: {
                propertyExample1: {
                    type: 'string',
                    default: 'testing collapsed'
                },
                propertyExample2: {
                    type: 'number',
                    multipleOf: 2
                }
            },
            collapsed: false
        },
        propertyOrderExample3: {
            type: 'object',
            title: 'propertyOrderExample3',
            description: 'an object propertyOrderExample',
            properties: {
                propertyExample1: {
                    type: 'string',
                    propertyOrder: 3,
                },
                propertyExample2: {
                    type: 'number',
                    propertyOrder: 1,
                },
                propertyExample3: {
                    type: 'number',
                    propertyOrder: 2,
                },
            },
            collapsed: false
        }
    }
};


export const sampleSchemaData = {
    // 'stringExample': 'Rebeca',
    // 'dateExample': '2017-11-11',
    // 'textareaExample': 'wire me, initial value, instead',
    // 'numberExample': 7,
    // 'integerExample': 100,
    // // 'booleanExample': false,
    // 'location': {
    //     'city': 'Boston',
    //     'state': 'MA',
    //     'Capital': false
    // },
    // 'pets': [
    //     {
    //         'type': 'dog',
    //         'name': 'Brian'
    //     }
    // ]
};
