using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


// 본래는 다이얼로그 조작을 위해 만들어진 명령어 체계
// Gcode라는 특수 커맨드를 한줄씩 실행하여 대응되는 행동을 실행한다
// 예를 들어 다이얼로그 매니저에서 "T1 ..." 명령어는 다이얼로그 박스에 텍스트를 출력하는 명령어이다
// 
// Gcode 커맨드는 "{명령어} {인자1} {인자2} ..." 형태로 이루어진다
// "{인자}"는 "{flag}{value}" 형태로 {flag}와 {value} 사이에 공백이 없어야 한다
// {flag} 영문 한 글자이며 대,소문자를 구분하지 않는다
// {value}는 문자열, 정수, 실수, 불리언 중 하나이다
// 문자열은 큰따옴표로 감싸져 있어야 한다
// 정수와 실수는 부호가 있을 수 있으며, 부호는 숫자 앞에 나타난다
// 불리언은 플래그를 표시했으면 True, 표시하지 않았으면 False로 간주한다
// ex) "T1 M"100_001" S100 F" -> { "T" : "100_001", "S" : 100, "F" : True, "O" : false}
// 
// Gcode 커맨드는 세미콜론(;)으로 주석을 달 수 있다
// 즉, 한 줄의 커맨드에서 세미콜론 이후의 내용은 무시된다
// ex) "T1 M"100_001" S100 F;<-여기부터 무시됨"


/// <summary>
/// Gcode 파싱을 위한 추상 클래스
/// 먼저 LoadGcode를 이용해 Gcode를 불러와야 한다
/// abstract void Execute()를 구현해야 한다
/// 
/// Gcode 커맨드를 파싱해주는 기본 메소드 GetArguments()를 제공한다
/// 
/// Gcode 인자는 Dictionary<string, object> 형태로 반환된다
/// ex) { "T" : "100_001", "S" : 100, "F" : True, "O" : false}
/// </summary>
public abstract class GcodeParserBase : MonoBehaviour
{
    public List<string> commands = new List<string>();
    private string currentCommand => commands[count];
    private int count;


    /// <summary>
    /// Gcode 파일을 불러온다
    /// </summary>
    /// <param name="filename"></param>
    public void LoadGcode(string filename)
    {
        filename = Application.streamingAssetsPath + "/Data/Dialogue/" + filename;
        string[] lines = System.IO.File.ReadAllLines(filename, System.Text.Encoding.UTF8);
        commands = new List<string>(lines);

        count = 0;
    }

    /// <summary>
    /// Gcode를 실행하는 추상 메소드
    /// </summary>
    public abstract void Execute();


    /// <summary>
    /// Gcode의 명령어 타입을 반환한다
    /// ex) "T1 M"100_001" S100 F" -> "T1"
    /// </summary>
    /// <returns></returns>
    public string GetCommandType()
    {
        if (commands == null)
        {
            return null;
        }
        string command = GetCommandWOSemicolon();
        if (command == null)
        {
            return null;
        }

        string[] tokens = command.Split(' ');
        if (tokens.Length == 0)
        {
            return null;
        }

        return tokens[0].ToUpper();
    }

    /// <summary>
    /// 다음 명령어로 넘어간다
    /// </summary>
    public void Next()
    {
        if (commands == null)
        {
            return;
        }
        count++;
    }

    /// <summary>
    /// Gcode를 비운다
    /// </summary>
    public void Flush()
    {
        commands = null;
        count = 0;
    }

    /// <summary>
    /// Gcode의 명령어 개수를 반환한다
    /// </summary>
    /// <returns></returns>
    public int GetLength()
    {
        if (commands == null)
        {
            return 0;
        }
        return commands.Count;
    }

    /// <summary>
    /// 현재 명령어를 반환한다
    /// ex) "T1 M"100_001" S100 F"
    /// </summary>
    /// <returns></returns>
    public string GetCommand()
    {
        if (commands == null)
        {
            return null;
        }
        return commands[count];
    }

    /// <summary>
    /// Gcode가 끝났는지 확인한다
    /// </summary>
    /// <returns></returns>
    public bool IsEnd()
    {
        if (commands == null)
        {
            return false;
        }
        return count == commands.Count;
    }

    /// <summary>
    /// Gcode의 인자를 반환한다
    /// 각 Flag에 대응되는 type을 Dictionary로 받아야 한다
    /// ex) { "T" : typeof(string), "S" : typeof(int), "F" : typeof(bool), "O" : typeof(bool) }
    /// 
    /// 해당 플래그가 있으면 해당 플래그에 대응하는 값을 반환한다
    /// ***반환 Dictionary의 Key는 대문자로 변환되어 반환된다***
    /// ***플래그가 없으면 Value = null을 반환한다***
    /// </summary>
    /// <param name="typeMapping">각 Flag에 대응되는 type</param>
    /// <returns>해당 플래그에 대응하는 값</returns>
    public Dictionary<string, object> GetArguments(Dictionary<string, Type> typeMapping)
    {
        Dictionary<string, object> arguments = new Dictionary<string, object>();
        if (commands == null)
        {
            return null;
        }

        string command = GetCommandWOSemicolon();

        if (command == null)
        {
            return null;
        }

        int s = 0;

        // Skip command
        while (s < command.Length && command[s] != ' ') { ++s; }

        try
        {
            while (s < command.Length)
            {
                if (command[s] == ' ')
                {
                    ++s;
                    continue;
                }

                if (command[s] == ';')
                {
                    break;
                }

                if (typeMapping.Keys.Contains(command[s].ToString().ToLower())
                || typeMapping.Keys.Contains(command[s].ToString().ToUpper()))
                {
                    string key = command[s].ToString().ToUpper();
                    ++s;

                    object value = null;
                    if (typeMapping[key] == typeof(string))
                    {
                        value = GetStringArg(command, ref s);
                    }
                    else if (typeMapping[key] == typeof(int))
                    {
                        value = GetIntArg(command, ref s);
                    }
                    else if (typeMapping[key] == typeof(float))
                    {
                        value = GetFloatArg(command, ref s);
                    }
                    else if (typeMapping[key] == typeof(bool))
                    {
                        value = true;
                    }
                    arguments[key] = value;
                    typeMapping.Remove(key);
                }
                else
                {
                    ++s;
                }
            }

        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }

        while (typeMapping.Count > 0)
        {
            string key = typeMapping.Keys.First();
            arguments[key] = null;
            typeMapping.Remove(key);
        }

        return arguments;

    }

    private string GetStringArg(string command, ref int index)
    {
        int start = -1;
        int end = -1;
        if (command == null)
        {
            return null;
        }

        while (index < command.Length)
        {
            if (command[index] == '\"')
            {
                if (start == -1)
                {
                    start = index + 1;
                }
                else
                {
                    end = index;
                    break;
                }
            }
            ++index;
        }

        if (start == -1 || end == -1)
        {
            return null;
        }

        return command[start..end];
    }

    private int GetIntArg(string command, ref int index)
    {
        int start = index;
        int end = -1;
        if (command == null)
        {
            return -1;
        }

        while (index < command.Length && command[index] != ' ')
        {
            ++index;
            end = index;
        }

        if (start == -1 || end == -1)
        {
            return -1;
        }

        return int.Parse(command[start..end]);
    }

    private float GetFloatArg(string command, ref int index)
    {
        int start = index;
        int end = -1;
        if (command == null)
        {
            return -1;
        }

        while (index < command.Length && command[index] != ' ')
        {
            ++index;
            end = index;
        }

        if (start == -1 || end == -1)
        {
            return -1;
        }

        return float.Parse(command[start..end]);
    }



    private string GetCommandWOSemicolon()
    {
        if (commands == null)
        {
            return null;
        }
        string command = currentCommand;
        for (int i = 0; i < command.Length; i++)
        {
            if (command[i] == ';')
            {
                command = command[..i];
                break;
            }
        }
        return command;
    }


}