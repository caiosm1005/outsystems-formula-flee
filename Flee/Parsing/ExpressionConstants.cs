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
 * Copyright (c) 2026 Caio Santana Magalhães
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
        LIKE = 1019,
        MATCH = 1020,
        CONTAINS = 1021,
        ANY = 1022,
        ALL = 1023,
        DOT = 1024,
        ARGUMENT_SEPARATOR = 1025,
        ARRAY_BRACES = 1026,
        SINGLE_LINE_COMMENT = 1027,
        MULTI_LINE_COMMENT = 1028,
        WHITESPACE = 1029,
        INTEGER = 1030,
        REAL = 1031,
        STRING_LITERAL = 1032,
        TRUE = 1033,
        FALSE = 1034,
        DATETIME = 1035,
        DATE = 1036,
        TIME = 1037,
        REGEXP = 1038,
        IDENTIFIER = 1039,
        IF = 1040,
        EXPRESSION = 2001,
        OR_EXPRESSION = 2002,
        AND_EXPRESSION = 2003,
        NOT_EXPRESSION = 2004,
        SQL_OP_EXPRESSION = 2005,
        SQL_OP_RHS = 2006,
        NEGATED_SQL_OP_RHS = 2007,
        IN_RHS = 2008,
        LIKE_RHS = 2009,
        MATCH_RHS = 2010,
        CONTAINS_RHS = 2011,
        CONTAINS_TARGET_EXPRESSION = 2012,
        CONTAINS_ARG_LIST = 2013,
        IN_TARGET_EXPRESSION = 2014,
        IN_LIST_TARGET_EXPRESSION = 2015,
        COMPARE_EXPRESSION = 2016,
        ADDITIVE_EXPRESSION = 2017,
        MULTIPLICATIVE_EXPRESSION = 2018,
        NEGATE_EXPRESSION = 2019,
        MEMBER_EXPRESSION = 2020,
        MEMBER_ACCESS_EXPRESSION = 2021,
        BASIC_EXPRESSION = 2022,
        MEMBER_FUNCTION_EXPRESSION = 2023,
        FIELD_PROPERTY_EXPRESSION = 2024,
        IF_EXPRESSION = 2025,
        INDEX_EXPRESSION = 2026,
        FUNCTION_CALL_EXPRESSION = 2027,
        ARGUMENT_LIST = 2028,
        LITERAL_EXPRESSION = 2029,
        BOOLEAN_LITERAL_EXPRESSION = 2030,
        EXPRESSION_GROUP = 2031
    }
}
