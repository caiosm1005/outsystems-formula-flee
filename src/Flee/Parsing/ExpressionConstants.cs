/*
 * ExpressionConstants.cs
 *
 * THIS FILE HAS BEEN GENERATED AUTOMATICALLY. DO NOT EDIT!
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free
 * Software Foundation, Inc., 59 Temple Place, Suite 330, Boston,
 * MA 02111-1307, USA.
 *
 *
 * Copyright (c) 2025 Caio Santana Magalhães
 */

namespace Flee.Parsing {

    /**
     * <remarks>An enumeration with token and production node
     * constants.</remarks>
     */
    internal enum ExpressionConstants {
        ADD = 1001,
        SUB = 1002,
        MUL = 1003,
        DIV = 1004,
        LEFT_PAREN = 1005,
        RIGHT_PAREN = 1006,
        LEFT_BRACE = 1007,
        RIGHT_BRACE = 1008,
        EQ = 1009,
        LT = 1010,
        GT = 1011,
        LTE = 1012,
        GTE = 1013,
        NE = 1014,
        AND = 1015,
        OR = 1016,
        NOT = 1017,
        IN = 1018,
        DOT = 1019,
        ARGUMENT_SEPARATOR = 1020,
        ARRAY_BRACES = 1021,
        SINGLE_LINE_COMMENT = 1022,
        MULTI_LINE_COMMENT = 1023,
        WHITESPACE = 1024,
        INTEGER = 1025,
        REAL = 1026,
        STRING_LITERAL = 1027,
        TRUE = 1028,
        FALSE = 1029,
        IDENTIFIER = 1030,
        DATE = 1031,
        DATETIME = 1032,
        TIME = 1033,
        IF = 1034,
        CAST = 1035,
        EXPRESSION = 2001,
        OR_EXPRESSION = 2002,
        AND_EXPRESSION = 2003,
        NOT_EXPRESSION = 2004,
        IN_EXPRESSION = 2005,
        IN_TARGET_EXPRESSION = 2006,
        IN_LIST_TARGET_EXPRESSION = 2007,
        COMPARE_EXPRESSION = 2008,
        ADDITIVE_EXPRESSION = 2009,
        MULTIPLICATIVE_EXPRESSION = 2010,
        NEGATE_EXPRESSION = 2011,
        MEMBER_EXPRESSION = 2012,
        MEMBER_ACCESS_EXPRESSION = 2013,
        BASIC_EXPRESSION = 2014,
        MEMBER_FUNCTION_EXPRESSION = 2015,
        FIELD_PROPERTY_EXPRESSION = 2016,
        SPECIAL_FUNCTION_EXPRESSION = 2017,
        IF_EXPRESSION = 2018,
        CAST_EXPRESSION = 2019,
        CAST_TYPE_EXPRESSION = 2020,
        INDEX_EXPRESSION = 2021,
        FUNCTION_CALL_EXPRESSION = 2022,
        ARGUMENT_LIST = 2023,
        LITERAL_EXPRESSION = 2024,
        BOOLEAN_LITERAL_EXPRESSION = 2025,
        EXPRESSION_GROUP = 2026
    }
}
