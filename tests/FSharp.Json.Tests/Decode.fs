module FSharp.Json.Tests.Decode

open FSharp.Json.Encode
open FSharp.Json.Decode
open Chessie.ErrorHandling
open NUnit.Framework

let (==) expected actual =
    match actual with
    | Ok (v, _) -> Assert.AreEqual(expected, v)
    | Bad err -> Assert.Fail(sprintf "%A" err)

[<Test>]
let ``returns "hello world"`` () =
    "hello world" == decodeValue dstring (jstring "hello world")

[<Test>]
let ``returns 42`` () =
    42 == decodeValue dint (jint 42)

[<Test>]
let ``returns 1.23123`` () =
    1.23123 == decodeValue dfloat (jfloat 1.23123)

[<Test>]
let ``returns true`` () =
    true == decodeValue dbool (jbool true)

[<Test>]
let ``returns false`` () =
    false == decodeValue dbool (jbool false)

[<Test>]
let ``returns 42 if null`` () =
    42 == decodeValue (dnull 42) jnull

[<Test>]
let ``returns maybe 42`` () =
    Some 42 == decodeValue (maybe dint) (jint 42)
    None == decodeValue (maybe dint) jnull

[<Test>]
let ``returns object1`` () =
    "foo" ==
        decodeString
            (object1 id
                     ("name" := dstring))
            "{ \"name\": \"foo\" }"

[<Test>]
let ``returns object2`` () =
    ("foo", 42) ==
        decodeString
            (object2 (fun name age -> name, age)
                     ("name" := dstring)
                     ("age" := dint))
            "{ \"name\": \"foo\", \"age\": 42 }"

[<Test>]
let ``returns integer list`` () =
    [1..10] ==
        decodeString
            (list dint)
            "[ 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 ]"

[<Test>]
let ``returns either foo or bar if empty`` () =
    let test input =
        decodeValue
            (oneOf [dstring; dnull "bar"])
            input
    "foo" == test (jstring "foo")
    "bar" == test (jstring "bar")
    "bar" == test jnull

[<Test>]
let ``returns keyvaluepairs`` () =
    [ ("name", "foo"); ("name2", "bar") ] ==
        decodeString
            (keyValuePairs dstring)
            "{ \"name\": \"foo\", \"name2\": \"bar\" }"

[<Test>]
let ``returns map`` () =
    Map.ofList [ ("name", "foo"); ("name2", "bar") ] ==
        decodeString
            (dmap dstring)
            "{ \"name\": \"foo\", \"name2\": \"bar\" }"

[<Test>]
let ``returns email and age at position`` () =
    let json = "{ \"person\": { \"contact\": { \"email\": \"email@example.com\" }, \"age\": 42 } }"
    let email =
        decodeString
            (at [ "person"; "contact"; "email" ] dstring)
            json
    "email@example.com" == email
    let age =
        decodeString
            (at [ "person"; "age" ] dint)
            json
    42 == age

[<Test>]
let ``returns tuple (foo, 42)`` () =
    ("foo", 42) ==
        decodeString
            (tuple2 (fun s i -> s, i) dstring dint)
            "[\"foo\", 42]"

[<Test>]
let ``returns tuple (foo, 42, baz)`` () =
    ("foo", 42, "baz") ==
        decodeString
            (tuple3 (fun s i s2 -> s, i, s2) dstring dint dstring)
            "[\"foo\", 42, \"baz\"]"

[<Test>]
let ``returns tuple (foo, 42, baz, false)`` () =
    ("foo", 42, "baz", false) ==
        decodeString
            (tuple4 (fun s i s2 b -> s, i, s2, b) dstring dint dstring dbool)
            "[\"foo\", 42, \"baz\", false]"

module Trial =
    let lift3 f res1 res2 res3 = trial {
        let! r1 = res1
        let! r2 = res2
        let! r3 = res3
        return f r1 r2 r3
    }

[<Test>]
let ``returns crazy formatted data`` () =
    let variadic2 (f: 'a -> 'b -> 'c list -> 'value) a b (cs: Decoder<'c>): Decoder<'value> =
        customDecoder (list value) (function
            | one::two::rest ->
                let rest' =
                    List.map (decodeValue cs) rest
                    |> Trial.collect
                Trial.lift3 f
                    (decodeValue a one)
                    (decodeValue b two)
                    rest'
            | _ -> fail "expecting at least two elements in array")
    (false, "test", [ 42; 12; 12 ]) ==
        decodeString
            (variadic2 (fun a b c -> a, b, c) dbool dstring dint)
            "[false, \"test\", 42, 12, 12]"

