namespace SKAIChips_Verification_Tool.RegisterControl.Core
{
    public static class RegisterMapParser
    {

        public static RegisterGroup MakeRegisterGroup(string groupName, string[,] regData)
        {
            var rg = new RegisterGroup(groupName);

            if (regData == null)
                return rg;

            var rowCount = regData.GetLength(0);
            var colCount = regData.GetLength(1);

            for (var xStart = 0; xStart < 3 && xStart < colCount; xStart++)
            {
                for (var row = 0; row < rowCount; row++)
                {
                    if (row < 1 || row + 2 >= rowCount)
                        continue;

                    if (regData[row, xStart] != "Bit" ||
                        regData[row + 1, xStart] != "Name" ||
                        regData[row + 2, xStart] != "Default")
                        continue;

                    string strAddr = null;

                    if (xStart + 1 < colCount)
                        strAddr = regData[row - 1, xStart + 1];

                    if (!string.IsNullOrWhiteSpace(strAddr) &&
                        strAddr.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    {
                        strAddr = strAddr.Substring(2);
                    }

                    if (!uint.TryParse(
                            strAddr,
                            System.Globalization.NumberStyles.HexNumber,
                            null,
                            out var address))
                    {
                        continue;
                    }

                    var regName = xStart + 2 < colCount ? regData[row - 1, xStart + 2] : null;
                    var reg = rg.AddRegister(regName, address);

                    uint resetValue = 0;

                    for (var column = xStart + 1; column < colCount; column++)
                    {
                        var defText = regData[row + 2, column];
                        var nameText = regData[row + 1, column];

                        if (defText == "X" ||
                            defText == "-" ||
                            defText == null ||
                            nameText == null)
                        {
                            continue;
                        }

                        var itemName = nameText;
                        string itemDesc = null;

                        if (!int.TryParse(regData[row, column], out var upperBit))
                            continue;

                        var lowerBit = upperBit;

                        uint itemValue = defText == "0" ? 0u : 1u;

                        for (var x = column + 1; x < colCount; x++)
                        {
                            if (regData[row + 1, x] != null)
                                break;

                            var bitText = regData[row, x];
                            if (bitText == null)
                                continue;

                            if (!int.TryParse(bitText, out var bit))
                                continue;

                            lowerBit = bit;

                            var bitDef = regData[row + 2, x];
                            itemValue = (itemValue << 1) | (bitDef == "0" ? 0u : 1u);
                        }

                        for (var y = row; y < rowCount; y++)
                        {
                            if (regData[y, xStart] != itemName)
                                continue;

                            if (xStart + 1 < colCount)
                                itemDesc = regData[y, xStart + 1];

                            for (var descRow = y + 1; descRow < rowCount; descRow++)
                            {
                                if (regData[descRow, xStart] != null)
                                    break;

                                var col3 = xStart + 3 < colCount ? regData[descRow, xStart + 3] : null;
                                var col4 = xStart + 4 < colCount ? regData[descRow, xStart + 4] : null;

                                var lineDesc = string.Empty;

                                if (col3 != null && col4 != null)
                                    lineDesc = "\n" + col3 + "=" + col4;
                                else if (col4 != null)
                                    lineDesc = "\n" + col4;
                                else if (col3 != null)
                                    lineDesc = "\n" + col3 + "=";

                                if (!string.IsNullOrEmpty(lineDesc))
                                    itemDesc += lineDesc;
                            }

                            break;
                        }

                        reg.AddItem(itemName, upperBit, lowerBit, itemValue, itemDesc);

                        var width = upperBit - lowerBit + 1;
                        if (width <= 0)
                            continue;

                        var mask = width >= 32 ? 0xFFFFFFFFu : ((1u << width) - 1u);
                        var val = itemValue & mask;

                        resetValue &= ~(mask << lowerBit);
                        resetValue |= val << lowerBit;
                    }

                    reg.ResetValue = resetValue;
                }
            }

            return rg;
        }

    }
}
